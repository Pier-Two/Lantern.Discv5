using Lantern.Discv5.Enr;

namespace Lantern.Discv5.WireProtocol.Table;

public interface IRoutingTable
{
    int GetTotalEntriesCount();
    
    int GetTotalActiveNodesCount();

    NodeTableEntry[] GetAllNodeEntries();
    
    IEnumerable<EnrRecord> GetBootstrapEnrs();

    void UpdateTable(EnrRecord enrRecord);

    void MarkNodeAsQueried(byte[] nodeId);
    
    void MarkNodeAsLive(byte[] nodeId);
    
    void MarkNodeAsDead(byte[] nodeId);

    void IncreaseFailureCounter(byte[] nodeId);

    void RefreshBuckets();

    NodeTableEntry? GetNodeEntry(byte[] nodeId);

    List<NodeTableEntry> GetClosestNodes(byte[] targetNodeId);

    List<EnrRecord> GetEnrRecordsAtDistances(IEnumerable<int> distances);
}