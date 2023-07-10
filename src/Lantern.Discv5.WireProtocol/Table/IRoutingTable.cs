using Lantern.Discv5.Enr;

namespace Lantern.Discv5.WireProtocol.Table;

public interface IRoutingTable
{
    TableOptions TableOptions { get; }
    
    int GetTotalEntriesCount();
    
    int GetTotalActiveNodesCount();

    NodeTableEntry[] GetAllNodeEntries();

    NodeTableEntry? GetLeastRecentlySeenNode();

    void UpdateFromEntry(NodeTableEntry nodeEntry);
    
    void UpdateFromEnr(EnrRecord enrRecord);

    void MarkNodeAsPending(byte[] nodeId);

    void MarkNodeAsLive(byte[] nodeId);
    
    void MarkNodeAsDead(byte[] nodeId);

    void IncreaseFailureCounter(byte[] nodeId);
    
    void PopulateFromBootstrapEnrs();

    NodeTableEntry? GetNodeEntry(byte[] nodeId);

    List<NodeTableEntry> GetClosestNodes(byte[] targetNodeId);

    List<EnrRecord> GetEnrRecordsAtDistances(IEnumerable<int> distances);
}