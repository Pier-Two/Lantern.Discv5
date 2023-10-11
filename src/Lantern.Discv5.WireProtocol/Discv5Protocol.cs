using Lantern.Discv5.Enr;
using Lantern.Discv5.WireProtocol.Connection;
using Lantern.Discv5.WireProtocol.Identity;
using Lantern.Discv5.WireProtocol.Message;
using Lantern.Discv5.WireProtocol.Packet;
using Lantern.Discv5.WireProtocol.Session;
using Lantern.Discv5.WireProtocol.Table;
using Microsoft.Extensions.Logging;

namespace Lantern.Discv5.WireProtocol;

public class Discv5Protocol(IConnectionManager connectionManager,
    IIdentityManager identityManager,
    ITableManager tableManager,
    IRequestManager requestManager,
    IPacketManager packetManager,
    IRoutingTable routingTable,
    ISessionManager sessionManager,
    ILookupManager lookupManager,
    ILogger<Discv5Protocol> logger)
{
    public IEnr SelfEnr => identityManager.Record;
    
    public int NodesCount => routingTable.GetNodesCount();
    
    public int PeerCount => routingTable.GetActiveNodesCount();
    
    public int ActiveSessionCount => sessionManager.TotalSessionCount;
    
    public NodeTableEntry? GetNodeFromId(byte[] nodeId) => routingTable.GetNodeEntry(nodeId);

    public NodeTableEntry[] GetAllNodes() => routingTable.GetAllNodes();

    public async Task StartProtocolAsync()
    {
        connectionManager.StartConnectionManagerAsync();
        requestManager.StartRequestManager();
        
        await tableManager.StartTableManagerAsync();
    }
    
    public async Task StopProtocolAsync()
    {
        var stopConnectionManagerTask = connectionManager.StopConnectionManagerAsync();
        var stopTableManagerTask = tableManager.StopTableManagerAsync();
        var stopRequestManagerTask = requestManager.StopRequestManagerAsync();
        
        await Task.WhenAll(stopConnectionManagerTask, stopTableManagerTask, stopRequestManagerTask).ConfigureAwait(false);
    }
    
    public event Action<NodeTableEntry> NodeAdded
    {
        add => routingTable.NodeAdded += value;
        remove => routingTable.NodeAdded -= value;
    }
    
    public event Action<NodeTableEntry> NodeRemoved
    {
        add => routingTable.NodeRemoved += value;
        remove => routingTable.NodeRemoved -= value;
    }
    
    public event Action<NodeTableEntry> NodeAddedToCache
    {
        add => routingTable.NodeAddedToCache += value;
        remove => routingTable.NodeAddedToCache -= value;
    }

    public event Action<NodeTableEntry> NodeRemovedFromCache
    {
        add => routingTable.NodeRemovedFromCache += value;
        remove => routingTable.NodeRemovedFromCache -= value;
    }

    public async Task<List<NodeTableEntry>?> PerformLookupAsync(byte[] targetNodeId)
    {
        if (routingTable.GetActiveNodesCount() > 0)
        {
            var closestNodes = await lookupManager.LookupAsync(targetNodeId);
            
            if (closestNodes != null)
            {
                return closestNodes;
            }
        }
        
        return null;
    }
    
    public async Task<bool> SendPingAsync(IEnr destination)
    {
        try
        {
            await packetManager.SendPacket(destination, MessageType.Ping);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred in SendPingAsync. Cannot send PING to {Record}", destination);
            return false;
        }
    }

    public async Task<bool> SendFindNodeAsync(Enr.Enr destination, byte[] targetNodeId)
    {
        try
        {
            await packetManager.SendPacket(destination, MessageType.FindNode, targetNodeId);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred in SendFindNodeAsync. Cannot send FINDNODE to {Record}", destination);
            return false;
        }
    }
    
    public async Task<bool> SendTalkReqAsync(Enr.Enr destination, byte[] protocol, byte[] request)
    {
        try
        {
            await packetManager.SendPacket(destination, MessageType.TalkReq, protocol, request);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred in SendTalkReqAsync. Cannot send TALKREQ to {Record}", destination);
            return false;
        }
    }
}
