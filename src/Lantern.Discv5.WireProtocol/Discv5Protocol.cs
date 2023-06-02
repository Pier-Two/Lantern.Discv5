using Lantern.Discv5.Enr;
using Lantern.Discv5.WireProtocol.Connection;
using Lantern.Discv5.WireProtocol.Discovery;
using Lantern.Discv5.WireProtocol.Identity;
using Lantern.Discv5.WireProtocol.Message;
using Lantern.Discv5.WireProtocol.Packet;
using Lantern.Discv5.WireProtocol.Session;
using Lantern.Discv5.WireProtocol.Table;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lantern.Discv5.WireProtocol;

public class Discv5Protocol
{
    private readonly IConnectionManager _connectionManager;
    private readonly IIdentityManager _identityManager;
    private readonly ITableManager _tableManager;
    private readonly IDiscoveryManager _discoveryManager;
    private readonly IRequestManager _requestManager;
    private readonly IPacketManager _packetManager;
    private readonly IRoutingTable _routingTable;
    private readonly ISessionManager _sessionManager;
    private readonly ILookupManager _lookupManager;
    private readonly ILogger<Discv5Protocol> _logger;
    
    public Discv5Protocol(IServiceProvider serviceProvider)
    {
        _connectionManager = serviceProvider.GetRequiredService<IConnectionManager>();
        _identityManager = serviceProvider.GetRequiredService<IIdentityManager>();
        _tableManager = serviceProvider.GetRequiredService<ITableManager>();
        _discoveryManager = serviceProvider.GetRequiredService<IDiscoveryManager>();
        _requestManager = serviceProvider.GetRequiredService<IRequestManager>();
        _packetManager = serviceProvider.GetRequiredService<IPacketManager>();
        _routingTable = serviceProvider.GetRequiredService<IRoutingTable>();
        _sessionManager = serviceProvider.GetRequiredService<ISessionManager>();
        _lookupManager = serviceProvider.GetRequiredService<ILookupManager>();
        _logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger<Discv5Protocol>();
    }
    
    public EnrRecord SelfEnrRecord => _identityManager.Record;
    
    public int TotalNodesCount() => _routingTable.GetTotalEntriesCount();
    
    public int PeerCount() => _routingTable.GetTotalActiveNodesCount();
    
    public int ActiveSessionCount() => _sessionManager.TotalSessionCount;
    
    public NodeTableEntry? GetNodeFromId(byte[] nodeId) => _routingTable.GetNodeEntry(nodeId);

    public NodeTableEntry[] GetAllNodes() => _routingTable.GetAllNodeEntries();

    public async Task StartProtocolAsync(CancellationToken token = default)
    {
        var connectionTask = _connectionManager.StartConnectionManagerAsync(token);
        var discoveryTask = _discoveryManager.StartDiscoveryManagerAsync(token);
        var tableTask = _tableManager.StartTableManagerAsync(token);
        var requestsTask = _requestManager.StartRequestManagerAsync(token);
        
        await Task.WhenAll(connectionTask, discoveryTask, tableTask, requestsTask).ConfigureAwait(false);
    }
    
    public async Task StopProtocolAsync(CancellationToken token = default)
    {
        await _connectionManager.StopConnectionManagerAsync(token);
        await _discoveryManager.StopDiscoveryManagerAsync(token);
        await _tableManager.StopTableManagerAsync(token);
        await _requestManager.StopRequestManagerAsync(token);
    }

    public async Task PerformLookup(byte[] targetNodeId)
    {
        _logger.LogInformation("Performing lookup...");
        /*var closestNodes = await _lookupManager.PerformLookup(targetNodeId);
        _logger.LogInformation("Lookup completed. Closest nodes found:");
        
        foreach (var node in closestNodes)
        {
            _logger.LogInformation("Node ID: {NodeId}", Convert.ToHexString(node.Id));
        }*/
    }
    
    public async Task SendPingAsync(EnrRecord destinationRecord)
    {
        try
        {
            await _packetManager.SendPingPacket(destinationRecord);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred in SendPingAsync. Cannot send PING to {Record}", destinationRecord);
        }
    }

    public async Task SendFindNodeAsync(byte[] nodeId, EnrRecord destinationRecord)
    {
        try
        {
            await _packetManager.SendFindNodePacket(destinationRecord, nodeId, false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred in SendFindNodeAsync. Cannot send FINDNODE to {Record}", destinationRecord);
        }
    }
    
    public async Task SendTalkReqAsync(byte[] data, EnrRecord destinationRecord)
    {
        
    }
    
    public async Task SendTalkRespAsync(byte[] data, EnrRecord destinationRecord)
    {
        
    }
}
