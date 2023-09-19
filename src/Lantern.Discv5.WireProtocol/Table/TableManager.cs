using Lantern.Discv5.Enr;
using Lantern.Discv5.WireProtocol.Identity;
using Lantern.Discv5.WireProtocol.Message;
using Lantern.Discv5.WireProtocol.Packet;
using Lantern.Discv5.WireProtocol.Utility;
using Microsoft.Extensions.Logging;

namespace Lantern.Discv5.WireProtocol.Table;

public class TableManager : ITableManager
{
    private readonly IPacketManager _packetManager;
    private readonly IIdentityManager _identityManager;
    private readonly ILookupManager _lookupManager;
    private readonly IRoutingTable _routingTable;
    private readonly TableOptions _tableOptions;
    private readonly IEnrFactory _enrFactory;
    private readonly ILogger<TableManager> _logger;
    private readonly ICancellationTokenSourceWrapper _shutdownCts;
    private readonly IGracefulTaskRunner _taskRunner;
    private Task? _refreshTask;
    private Task? _pingTask;
    
    public TableManager(
        IPacketManager packetManager, 
        IIdentityManager identityManager, 
        ILookupManager lookupManager, 
        IRoutingTable routingTable, 
        IEnrFactory enrFactory, 
        ILoggerFactory loggerFactory, 
        ICancellationTokenSourceWrapper cts, 
        IGracefulTaskRunner taskRunner, 
        TableOptions tableOptions)
    {
        _packetManager = packetManager;
        _identityManager = identityManager;
        _lookupManager = lookupManager;
        _routingTable = routingTable;
        _tableOptions = tableOptions;
        _enrFactory = enrFactory;
        _logger = loggerFactory.CreateLogger<TableManager>();
        _taskRunner = taskRunner;
        _shutdownCts = cts;
    }

    public async Task StartTableManagerAsync()
    {
        _logger.LogInformation("Starting TableManagerAsync");
        
        await InitialiseFromBootstrapAsync();
        _refreshTask = _taskRunner.RunWithGracefulCancellationAsync(RefreshBucketsAsync, "RefreshBuckets", _shutdownCts.GetToken());
        _pingTask = _taskRunner.RunWithGracefulCancellationAsync(PingNodeAsync, "PingNode", _shutdownCts.GetToken());
    }
    
    public async Task StopTableManagerAsync()
    {
        _logger.LogInformation("Stopping TableManagerAsync");
        _shutdownCts.Cancel();

        await Task.WhenAll(_refreshTask, _pingTask).ConfigureAwait(false);
	
        if (_shutdownCts.IsCancellationRequested())
        {
            _logger.LogInformation("TableManagerAsync was canceled gracefully");
        }
    }
    
    public async Task InitialiseFromBootstrapAsync()
    {
        if (_routingTable.GetNodesCount() == 0)
        {
            _logger.LogInformation("Initialising from bootstrap ENRs");
            
            var bootstrapEnrs = _routingTable.TableOptions.BootstrapEnrs
                .Select(enr => _enrFactory.CreateFromString(enr, _identityManager.Verifier))
                .ToArray();

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

    public async Task RefreshBucketsAsync(CancellationToken token)
    {
        _logger.LogInformation("Starting RefreshBucketsAsync");
    
        while (!token.IsCancellationRequested)
        {
            await RefreshBucket().ConfigureAwait(false);
            await Task.Delay(_tableOptions.RefreshIntervalMilliseconds, token).ConfigureAwait(false);
        }
    }
    
    public async Task PingNodeAsync(CancellationToken token)
    {
        _logger.LogInformation("Starting PingNodeAsync");
    
        while (!token.IsCancellationRequested)
        {
            if (_routingTable.GetNodesCount() <= 0) 
                continue;
        
            await Task.Delay(_tableOptions.PingIntervalMilliseconds, token).ConfigureAwait(false);
        
            var targetNodeId = RandomUtility.GenerateRandomData(PacketConstants.NodeIdSize);
            var nodeEntry = _routingTable.GetClosestNodes(targetNodeId).FirstOrDefault();
        
            if (nodeEntry == null)
                continue;

            await _packetManager.SendPacket(nodeEntry.Record, MessageType.Ping);
        }
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
                _routingTable.UpdateFromEnr(node.Record);
            }
        }
    }
}