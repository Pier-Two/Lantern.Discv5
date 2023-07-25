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

        await Task.WhenAll(_listenTask, _handleTask).ConfigureAwait(false);
    
        if (_cts.IsCancellationRequested())
        {
            _logger.LogInformation("ConnectionManagerAsync was canceled gracefully");
        }
	
        _connection.Close();        
    }
    
    private async Task HandleIncomingPacketsAsync(CancellationToken token)
    {
        _logger.LogInformation("Starting HandleIncomingPacketsAsync");

        await foreach (var packet in _connection.ReadMessagesAsync(token).ConfigureAwait(false))
        {
            await _packetManager.HandleReceivedPacket(packet).ConfigureAwait(false);
        }
    }
}
