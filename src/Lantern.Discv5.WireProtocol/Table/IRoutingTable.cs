using Lantern.Discv5.Enr;

namespace Lantern.Discv5.WireProtocol.Table;

public interface IRoutingTable
{
    event Action<NodeTableEntry> NodeAdded;
    
    event Action<NodeTableEntry> NodeRemoved;
    
    event Action<NodeTableEntry> NodeAddedToCache;
    
    event Action<NodeTableEntry> NodeRemovedFromCache;
    
    TableOptions TableOptions { get; }
    
    int GetTotalEntriesCount();
    
    int GetTotalActiveNodesCount();

    NodeTableEntry[] GetAllNodeEntries();

    NodeTableEntry? GetLeastRecentlySeenNode();
    
    void UpdateFromEnr(IEnrRecord enrRecord);
    
    void MarkNodeAsResponded(byte[] nodeId);

    void MarkNodeAsPending(byte[] nodeId);

    void MarkNodeAsLive(byte[] nodeId);
    
    void MarkNodeAsDead(byte[] nodeId);

    void IncreaseFailureCounter(byte[] nodeId);
    
    void PopulateFromBootstrapEnrs();

    NodeTableEntry? GetNodeEntry(byte[] nodeId);

    List<NodeTableEntry> GetClosestNodes(byte[] targetNodeId);

    List<IEnrRecord> GetEnrRecordsAtDistances(IEnumerable<int> distances);
}