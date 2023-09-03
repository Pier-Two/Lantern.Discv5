using Lantern.Discv5.Enr;
using Lantern.Discv5.WireProtocol.Connection;
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
    private readonly IRequestManager _requestManager;
    private readonly IPacketManager _packetManager;
    private readonly IRoutingTable _routingTable;
    private readonly ISessionManager _sessionManager;
    private readonly ILookupManager _lookupManager;
    private readonly ILogger<Discv5Protocol> _logger;
    
    public Discv5Protocol(
        IConnectionManager connectionManager,
        IIdentityManager identityManager,
        ITableManager tableManager,
        IRequestManager requestManager,
        IPacketManager packetManager,
        IRoutingTable routingTable,
        ISessionManager sessionManager,
        ILookupManager lookupManager,
        ILogger<Discv5Protocol> logger)
    {
        _connectionManager = connectionManager;
        _identityManager = identityManager;
        _tableManager = tableManager;
        _requestManager = requestManager;
        _packetManager = packetManager;
        _routingTable = routingTable;
        _sessionManager = sessionManager;
        _lookupManager = lookupManager;
        _logger = logger;
    }
    
    public IEnrRecord SelfEnrRecord => _identityManager.Record;
    
    public int NodesCount => _routingTable.GetTotalEntriesCount();
    
    public int PeerCount => _routingTable.GetTotalActiveNodesCount();
    
    public int ActiveSessionCount => _sessionManager.TotalSessionCount;
    
    public NodeTableEntry? GetNodeFromId(byte[] nodeId) => _routingTable.GetNodeEntry(nodeId);

    public NodeTableEntry[] GetAllNodes() => _routingTable.GetAllNodeEntries();

    public void StartProtocol()
    {
        _connectionManager.StartConnectionManagerAsync();
        _tableManager.StartTableManagerAsync();
        _requestManager.StartRequestManager();
    }
    
    public async Task StopProtocolAsync()
    {
        var stopConnectionManagerTask = _connectionManager.StopConnectionManagerAsync();
        var stopTableManagerTask = _tableManager.StopTableManagerAsync();
        var stopRequestManagerTask = _requestManager.StopRequestManagerAsync();
        
        await Task.WhenAll(stopConnectionManagerTask, stopTableManagerTask, stopRequestManagerTask).ConfigureAwait(false);
    }
    
    public event Action<NodeTableEntry> NodeAdded
    {
        add => _routingTable.NodeAdded += value;
        remove => _routingTable.NodeAdded -= value;
    }
    
    public event Action<NodeTableEntry> NodeRemoved
    {
        add => _routingTable.NodeRemoved += value;
        remove => _routingTable.NodeRemoved -= value;
    }
    
    public event Action<NodeTableEntry> NodeAddedToCache
    {
        add => _routingTable.NodeAddedToCache += value;
        remove => _routingTable.NodeAddedToCache -= value;
    }

    public event Action<NodeTableEntry> NodeRemovedFromCache
    {
        add => _routingTable.NodeRemovedFromCache += value;
        remove => _routingTable.NodeRemovedFromCache -= value;
    }

    public async Task<List<NodeTableEntry>?> PerformLookupAsync(byte[] targetNodeId)
    {
        if (_routingTable.GetTotalActiveNodesCount() > 0)
        {
            _logger.LogInformation("Performing lookup...");
            var closestNodes = await _lookupManager.LookupAsync(targetNodeId);
            
            if (closestNodes != null)
            {
                return closestNodes;
            }
        }
        
        return null;
    }
    
    public async Task<bool> SendPingAsync(IEnrRecord destinationRecord)
    {
        try
        {
            await _packetManager.SendPacket(destinationRecord, MessageType.Ping);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred in SendPingAsync. Cannot send PING to {Record}", destinationRecord);
            return false;
        }
    }

    public async Task<bool> SendFindNodeAsync(EnrRecord destinationRecord, byte[] nodeId)
    {
        try
        {
            await _packetManager.SendPacket(destinationRecord, MessageType.FindNode, nodeId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred in SendFindNodeAsync. Cannot send FINDNODE to {Record}", destinationRecord);
            return false;
        }
    }
    
    public async Task<bool> SendTalkReqAsync(EnrRecord destinationRecord, byte[] protocol, byte[] request)
    {
        try
        {
            await _packetManager.SendPacket(destinationRecord, MessageType.TalkReq, protocol, request);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred in SendTalkReqAsync. Cannot send TALKREQ to {Record}", destinationRecord);
            return false;
        }
    }
    
    public async Task<bool> SendTalkRespAsync(EnrRecord destinationRecord, byte[] response)
    {
        try
        {
            await _packetManager.SendPacket(destinationRecord, MessageType.TalkResp, response);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred in SendTalkRespAsync. Cannot send TALKRESP to {Record}", destinationRecord);
            return false;
        }
    }
}
