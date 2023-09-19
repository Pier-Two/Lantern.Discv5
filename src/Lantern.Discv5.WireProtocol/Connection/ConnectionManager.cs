using Lantern.Discv5.WireProtocol.Packet;
using Lantern.Discv5.WireProtocol.Utility;
using Microsoft.Extensions.Logging;

namespace Lantern.Discv5.WireProtocol.Connection;

public sealed class ConnectionManager : IConnectionManager
{
    private readonly IPacketManager _packetManager;
    private readonly IUdpConnection _connection;
    private readonly ILogger<ConnectionManager> _logger;
    private readonly ICancellationTokenSourceWrapper _cts;
    private readonly IGracefulTaskRunner _taskRunner;
    private Task? _listenTask;
    private Task? _handleTask;

    public ConnectionManager(IPacketManager packetManager, IUdpConnection connection, ICancellationTokenSourceWrapper cts, IGracefulTaskRunner taskRunner, ILoggerFactory loggerFactory)
    {
        _packetManager = packetManager;
        _connection = connection;
        _logger = loggerFactory.CreateLogger<ConnectionManager>();
        _cts = cts;
        _taskRunner = taskRunner;
    }

    public void StartConnectionManagerAsync()
    {
        _logger.LogInformation("Starting ConnectionManagerAsync");
    
        _listenTask = _taskRunner.RunWithGracefulCancellationAsync(_connection.ListenAsync, "Listen", _cts.GetToken());
        _handleTask = _taskRunner.RunWithGracefulCancellationAsync(HandleIncomingPacketsAsync, "HandleIncomingPackets", _cts.GetToken());
    }

    public async Task StopConnectionManagerAsync()
    {
        _logger.LogInformation("Stopping ConnectionManagerAsync");
        _cts.Cancel();

        try
        {
            if (_listenTask != null && _handleTask != null)
            {
                await Task.WhenAll(_listenTask, _handleTask).ConfigureAwait(false); 
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while waiting for tasks in StopConnectionManagerAsync: {Message}", ex.Message);
            throw;
        }
    
        if (_cts.IsCancellationRequested())
        {
            _logger.LogInformation("ConnectionManagerAsync was canceled gracefully");
        }
    
        try
        {
            _connection.Close(); 
        }  
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while closing the connection: {Message}", ex.Message);
            throw;
        }           
    }
    
    public async Task HandleIncomingPacketsAsync(CancellationToken token)
    {
        _logger.LogInformation("Starting HandleIncomingPacketsAsync");

        try
        {
            await foreach (var packet in _connection.ReadMessagesAsync(token).ConfigureAwait(false))
            {
                try 
                {
                    await _packetManager.HandleReceivedPacket(packet).ConfigureAwait(false);
                }
                catch (Exception ex)
                {         
                    _logger.LogError(ex, "Failed to handle incoming packet: {Packet}. Error: {Message}", packet, ex.Message);    
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("HandleIncomingPacketsAsync has been cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in HandleIncomingPacketsAsync: {Message}", ex.Message);
            throw; 
        }
    }
}
