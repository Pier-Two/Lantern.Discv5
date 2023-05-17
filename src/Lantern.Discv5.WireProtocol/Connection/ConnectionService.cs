using Lantern.Discv5.WireProtocol.Packet;
using Microsoft.Extensions.Logging;

namespace Lantern.Discv5.WireProtocol.Connection;

public sealed class ConnectionService : IConnectionService
{
    private readonly IPacketService _packetService;
    private readonly IUdpConnection _connection;
    private readonly CancellationTokenSource _shutdownCts;
    private readonly ILogger<ConnectionService> _logger;
    private Task? _listenTask;
    private Task? _handleTask;

    public ConnectionService(IPacketService packetService, IUdpConnection connection, ILoggerFactory loggerFactory)
    {
        _packetService = packetService;
        _connection = connection;
        _shutdownCts = new CancellationTokenSource();
        _logger = loggerFactory.CreateLogger<ConnectionService>();
    }

    public async Task StartConnectionServiceAsync(CancellationToken token = default)
    {
        _logger.LogInformation("Starting ConnectionServicesAsync");
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(token, _shutdownCts.Token);

        try
        {
            _listenTask = _connection.ListenAsync(linkedCts.Token);
            _handleTask = HandleIncomingPacketsAsync(linkedCts.Token);

            await Task.WhenAll(_listenTask, _handleTask).ConfigureAwait(false);
        }
        finally
        {
            linkedCts.Dispose();
        }
    }

    public async Task StopConnectionServiceAsync(CancellationToken token = default)
    {
        _logger.LogInformation("Stopping ConnectionServicesAsync");
        _shutdownCts.Cancel();

        try
        {
            if (_listenTask != null && _handleTask != null)
            {
                await Task.WhenAll(_listenTask, _handleTask).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested || _shutdownCts.IsCancellationRequested)
        {
            _logger.LogInformation("ConnectionServicesAsync was canceled gracefully");
        }
        finally
        {
            _connection.CompleteMessageChannel();
        }
    }

    private async Task HandleIncomingPacketsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting HandleIncomingPacketsAsync");

        try
        {
            await foreach (var packet in _connection.ReadMessagesAsync(cancellationToken).WithCancellation(cancellationToken))
            {
                await _packetService.HandleReceivedPacket(packet).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested || _shutdownCts.IsCancellationRequested)
        {
            _logger.LogInformation("HandleIncomingPacketsAsync was canceled gracefully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred in HandleIncomingPacketsAsync");
        }

        _logger.LogInformation("HandleIncomingPacketsAsync completed");
    }
}