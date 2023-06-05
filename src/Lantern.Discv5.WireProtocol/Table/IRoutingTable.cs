using Lantern.Discv5.Enr;

namespace Lantern.Discv5.WireProtocol.Table;

public interface IRoutingTable
{
    int GetTotalEntriesCount();
    
    int GetTotalActiveNodesCount();

    NodeTableEntry[] GetAllNodeEntries();
    
    IEnumerable<EnrRecord> GetBootstrapEnrs();

    NodeTableEntry? GetLeastRecentlySeenNode();

    void UpdateFromEntry(NodeTableEntry nodeEntry);
    
    void UpdateFromEnr(EnrRecord enrRecord);

    void MarkNodeAsQueried(byte[] nodeId);
    
    void MarkNodeAsLive(byte[] nodeId);
    
    void MarkNodeAsDead(byte[] nodeId);

    void IncreaseFailureCounter(byte[] nodeId);

    NodeTableEntry? GetNodeEntry(byte[] nodeId);

    List<NodeTableEntry> GetClosestNodes(byte[] targetNodeId);

    List<EnrRecord> GetEnrRecordsAtDistances(IEnumerable<int> distances);
}