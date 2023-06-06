using Lantern.Discv5.WireProtocol.Packet;
using Lantern.Discv5.WireProtocol.Utility;
using Microsoft.Extensions.Logging;

namespace Lantern.Discv5.WireProtocol.Table;

public class TableManager : ITableManager
{
    private readonly IPacketManager _packetManager;
    private readonly ILookupManager _lookupManager;
    private readonly IRoutingTable _routingTable;
    private readonly TableOptions _tableOptions;
    private readonly ILogger<TableManager> _logger;
    private readonly CancellationTokenSource _shutdownCts;
    private Task? _initializeTask;
    private Task? _refreshTask;
    private Task? _pingTask;
    
    public TableManager(IPacketManager packetManager, ILookupManager lookupManager, IRoutingTable routingTable, ILoggerFactory loggerFactory, TableOptions tableOptions)
    {
        _packetManager = packetManager;
        _lookupManager = lookupManager;
        _routingTable = routingTable;
        _tableOptions = tableOptions;
        _logger = loggerFactory.CreateLogger<TableManager>();
        _shutdownCts = new CancellationTokenSource();
    }
    
    public void StartTableManagerAsync()
    {
        _logger.LogInformation("Starting TableManagerAsync");
        _initializeTask = InitialiseFromBootstrapAsync();
        _refreshTask = RefreshBucketsAsync(_shutdownCts.Token);
        _pingTask = PingNodeAsync(_shutdownCts.Token);
    }
    
    public async Task StopTableManagerAsync()
    {
        _logger.LogInformation("Stopping TableManagerAsync");
        _shutdownCts.Cancel();
        
        try
        {
            if (_initializeTask != null && _refreshTask != null && _pingTask != null)
            {
                await Task.WhenAll(_initializeTask, _refreshTask, _pingTask).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (_shutdownCts.IsCancellationRequested)
        {
            _logger.LogInformation("TableManagerAsync was canceled gracefully");
        }
    }
    
    private async Task InitialiseFromBootstrapAsync()
    {
        if (_routingTable.GetTotalEntriesCount() == 0)
        {
            _logger.LogInformation("Initialising from bootstrap ENRs");
            var bootstrapEnrs = _routingTable.GetBootstrapEnrs();

            foreach (var bootstrapEnr in bootstrapEnrs)
            {
                try
                {
                    await _packetManager.SendPingPacket(bootstrapEnr);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error sending packet to bootstrap ENR: {BootstrapEnr}", bootstrapEnr);
                }
            }
        }
    }

    private async Task RefreshBucketsAsync(CancellationToken token)
    {
        _logger.LogInformation("Starting RefreshBucketsAsync");
        
        try
        {
            while (!token.IsCancellationRequested)
            {
                await RefreshBucket();
                await Task.Delay(_tableOptions.RefreshIntervalMilliseconds, token)
                    .ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (_shutdownCts.IsCancellationRequested)
        {
            _logger.LogInformation("RefreshBucketsAsync was canceled gracefully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred in RefreshBucketsAsync");
        }
        
        _logger.LogInformation("RefreshBucketsAsync completed");
    }
    
    private async Task PingNodeAsync(CancellationToken token)
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
        catch (OperationCanceledException) when (_shutdownCts.IsCancellationRequested)
        {
            _logger.LogInformation("PingNodeAsync was canceled gracefully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred in PingNodeAsync");
        }
        
        _logger.LogInformation("PingNodeAsync completed");
    }

    private async Task RefreshBucket()
    {
        var targetNodeId = _routingTable.GetLeastRecentlySeenNode();

        if (targetNodeId == null)
            return;

        var closestNodes = await _lookupManager.LookupAsync(targetNodeId.Id);

        if (closestNodes != null)
        {
            foreach(var node in closestNodes)
            {
                _routingTable.UpdateFromEntry(node);
            }
        }
    }
}