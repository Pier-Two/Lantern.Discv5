using Lantern.Discv5.Enr;

namespace Lantern.Discv5.WireProtocol.Table;

public interface IRoutingTable
{
    int GetTotalEntriesCount();
    
    IEnumerable<EnrRecord> GetBootstrapEnrs();

    void UpdateTable(EnrRecord enrRecord);

    void MarkNodeAsQueried(byte[] nodeId);

    void RefreshBuckets();

    NodeTableEntry? GetNodeEntry(byte[] nodeId);

    List<NodeTableEntry> GetInitialNodesForLookup(byte[] targetNodeId);

    NodeTableEntry GetNodeForFindNode(byte[] targetNodeId);

    int[] GetClosestNeighbours(byte[] targetNodeId);

    List<EnrRecord> GetEnrRecordsAtDistances(IEnumerable<int> distances);
}