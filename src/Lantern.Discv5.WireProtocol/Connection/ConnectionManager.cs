using Lantern.Discv5.WireProtocol.Packet;
using Microsoft.Extensions.Logging;

namespace Lantern.Discv5.WireProtocol.Connection;

public sealed class ConnectionManager : IConnectionManager
{
    private readonly IPacketManager _packetManager;
    private readonly IUdpConnection _connection;
    private readonly ILogger<ConnectionManager> _logger;
    private readonly CancellationTokenSource _shutdownCts;
    private Task? _listenTask;
    private Task? _handleTask;

    public ConnectionManager(IPacketManager packetManager, IUdpConnection connection, ILoggerFactory loggerFactory)
    {
        _packetManager = packetManager;
        _connection = connection;
        _logger = loggerFactory.CreateLogger<ConnectionManager>();
        _shutdownCts = new CancellationTokenSource();
    }

    public void StartConnectionManagerAsync()
    {
        _logger.LogInformation("Starting ConnectionManagerAsync");
    
        _listenTask = _connection.ListenAsync(_shutdownCts.Token);
        _handleTask = HandleIncomingPacketsAsync(_shutdownCts.Token);
    }

    public async Task StopConnectionManagerAsync()
    {
        _logger.LogInformation("Stopping ConnectionManagerAsync");
        _shutdownCts.Cancel();

        try
        {
            if (_listenTask != null && _handleTask != null)
            {
                await Task.WhenAll(_listenTask, _handleTask).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (_shutdownCts.IsCancellationRequested)
        {
            _logger.LogInformation("ConnectionManagerAsync was canceled gracefully");
        }
        finally
        {
            _connection.CompleteMessageChannel();
        }
    }

    private async Task HandleIncomingPacketsAsync(CancellationToken token)
    {
        _logger.LogInformation("Starting HandleIncomingPacketsAsync");

        try
        {
            await foreach (var packet in _connection.ReadMessagesAsync(token).ConfigureAwait(false))
            {
                await _packetManager.HandleReceivedPacket(packet).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (_shutdownCts.IsCancellationRequested)
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
