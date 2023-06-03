using Lantern.Discv5.WireProtocol.Packet;
using Lantern.Discv5.WireProtocol.Utility;
using Microsoft.Extensions.Logging;

namespace Lantern.Discv5.WireProtocol.Table;

public class TableManager : ITableManager
{
    private readonly IPacketManager _packetManager;
    private readonly IRoutingTable _routingTable;
    private readonly TableOptions _tableOptions;
    private readonly ILogger<TableManager> _logger;
    private Task? _refreshTask;
    private Task? _pingTask;
    
    public TableManager(IPacketManager packetManager, IRoutingTable routingTable, ILoggerFactory loggerFactory, TableOptions tableOptions)
    {
        _packetManager = packetManager;
        _routingTable = routingTable;
        _tableOptions = tableOptions;
        _logger = loggerFactory.CreateLogger<TableManager>();
    }
    
    public void StartTableManagerAsync(CancellationToken token = default)
    {
        _logger.LogInformation("Starting TableManagerAsync");
        _refreshTask = RefreshBucketsAsync(token);
        _pingTask = PingNodeAsync(token);
    }
    
    public async Task StopTableManagerAsync(CancellationToken token = default)
    {
        _logger.LogInformation("Stopping TableManagerAsync");

        try
        {
            if (_refreshTask != null && _pingTask != null)
            {
                await Task.WhenAll(_refreshTask, _pingTask).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            _logger.LogInformation("TableManagerAsync was canceled gracefully");
        }
    }
    
    private async Task RefreshBucketsAsync(CancellationToken token = default)
    {
        _logger.LogInformation("Starting RefreshBucketsAsync");
        
        try
        {
            while (!token.IsCancellationRequested)
            {
                _routingTable.RefreshBuckets();
                await Task.Delay(_tableOptions.RefreshIntervalMilliseconds, token)
                    .ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            _logger.LogInformation("RefreshBucketsAsync was canceled gracefully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred in RefreshBucketsAsync");
        }
        
        _logger.LogInformation("RefreshBucketsAsync completed");
    }
    
    private async Task PingNodeAsync(CancellationToken token = default)
    {
        _logger.LogInformation("Starting PingNodeAsync");
        
        try
        {
            while (!token.IsCancellationRequested)
            {
                if (_routingTable.GetTotalEntriesCount() <= 0) 
                    continue;
                
                await Task.Delay(_tableOptions.PingIntervalMilliseconds, token).ConfigureAwait(false);
                
                var targetNodeId = RandomUtility.GenerateNodeId(PacketConstants.NodeIdSize);
                var nodeEntry = _routingTable.GetClosestNodes(targetNodeId).First();

                await _packetManager.SendPingPacket(nodeEntry.Record);
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            _logger.LogInformation("PingNodeAsync was canceled gracefully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred in PingNodeAsync");
        }
        
        _logger.LogInformation("PingNodeAsync completed");
    }
}