using Lantern.Discv5.WireProtocol.Message;
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
        ShutdownCts = new CancellationTokenSource();
    }
    
    public CancellationTokenSource ShutdownCts { get; }

    public void StartTableManagerAsync()
    {
        _logger.LogInformation("Starting TableManagerAsync");
        _initializeTask = InitialiseFromBootstrapAsync();
        _refreshTask = RefreshBucketsAsync();
        _pingTask = PingNodeAsync();
    }
    
    public async Task StopTableManagerAsync()
    {
        _logger.LogInformation("Stopping TableManagerAsync");
        ShutdownCts.Cancel();
        
        try
        {
            if (_initializeTask != null && _refreshTask != null && _pingTask != null)
            {
                await Task.WhenAll(_initializeTask, _refreshTask, _pingTask).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (ShutdownCts.IsCancellationRequested)
        {
            _logger.LogInformation("TableManagerAsync was canceled gracefully");
        }
    }
    
    public async Task InitialiseFromBootstrapAsync()
    {
        if (_routingTable.GetTotalEntriesCount() == 0)
        {
            _logger.LogInformation("Initialising from bootstrap ENRs");
            _routingTable.PopulateFromBootstrapEnrs();

            var bootstrapEnrs = _routingTable.TableOptions.BootstrapEnrs;

            foreach (var bootstrapEnr in bootstrapEnrs)
            {
                try
                {
                    await _packetManager.SendPacket(bootstrapEnr, MessageType.Ping);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error sending packet to bootstrap ENR: {BootstrapEnr}", bootstrapEnr);
                }
            }
        }
    }

    public async Task RefreshBucketsAsync()
    {
        _logger.LogInformation("Starting RefreshBucketsAsync");
        
        try
        {
            while (!ShutdownCts.IsCancellationRequested)
            {
                await RefreshBucket();
                await Task.Delay(_tableOptions.RefreshIntervalMilliseconds, ShutdownCts.Token)
                    .ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (ShutdownCts.IsCancellationRequested)
        {
            _logger.LogInformation("RefreshBucketsAsync was canceled gracefully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred in RefreshBucketsAsync");
        }
        
        _logger.LogInformation("RefreshBucketsAsync completed");
    }
    
    public async Task PingNodeAsync()
    {
        _logger.LogInformation("Starting PingNodeAsync");
        
        try
        {
            while (!ShutdownCts.IsCancellationRequested)
            {
                if (_routingTable.GetTotalEntriesCount() <= 0) 
                    continue;
                
                await Task.Delay(_tableOptions.PingIntervalMilliseconds, ShutdownCts.Token).ConfigureAwait(false);
                
                var targetNodeId = RandomUtility.GenerateRandomData(PacketConstants.NodeIdSize);
                var nodeEntry = _routingTable.GetClosestNodes(targetNodeId).FirstOrDefault();
                
                if (nodeEntry == null)
                    continue;

                await _packetManager.SendPacket(nodeEntry.Record, MessageType.Ping);
            }
        }
        catch (OperationCanceledException) when (ShutdownCts.IsCancellationRequested)
        {
            _logger.LogInformation("PingNodeAsync was canceled gracefully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred in PingNodeAsync");
        }
        
        _logger.LogInformation("PingNodeAsync completed");
    }

    public async Task RefreshBucket()
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