using Lantern.Discv5.WireProtocol.Packet;
using Microsoft.Extensions.Logging;

namespace Lantern.Discv5.WireProtocol.Connection;

public sealed class ConnectionManager : IConnectionManager
{
    private readonly IPacketManager _packetManager;
    private readonly IUdpConnection _connection;
    private readonly ILogger<ConnectionManager> _logger;
    private Task? _listenTask;
    private Task? _handleTask;

    public ConnectionManager(IPacketManager packetManager, IUdpConnection connection, ILoggerFactory loggerFactory)
    {
        _packetManager = packetManager;
        _connection = connection;
        _logger = loggerFactory.CreateLogger<ConnectionManager>();
    }

    public void StartConnectionManagerAsync(CancellationToken token = default)
    {
        _logger.LogInformation("Starting ConnectionManager");
    
        _listenTask = _connection.ListenAsync(token);
        _handleTask = HandleIncomingPacketsAsync(token);
    }

    public async Task StopConnectionManagerAsync(CancellationToken token = default)
    {
        _logger.LogInformation("Stopping ConnectionServicesAsync");

        try
        {
            if (_listenTask != null && _handleTask != null)
            {
                await Task.WhenAll(_listenTask, _handleTask).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
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
                await _packetManager.HandleReceivedPacket(packet).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
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
