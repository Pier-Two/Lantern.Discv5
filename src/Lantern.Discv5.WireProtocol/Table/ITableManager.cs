using Lantern.Discv5.Enr;

namespace Lantern.Discv5.WireProtocol.Table;

public interface ITableManager
{
    public int RecordCount { get; }
    
    public IEnumerable<EnrRecord> GetBootstrapEnrs();
    
    public void UpdateTable(NodeTableEntry nodeEntry);
    
    public void UpdateTable(EnrRecord enrRecord);

    public void MarkNodeAsQueried(byte[] nodeId);

    public void RefreshTable();
    
    public NodeTableEntry? GetNodeEntry(byte[] nodeId);

    public IEnumerable<NodeTableEntry> GetInitialNodesForLookup(byte[] targetNodeId);

    public NodeTableEntry GetNodeForFindNode(byte[] targetNodeId);
    
    public List<EnrRecord> GetEnrRecordsAtDistances(IEnumerable<int> distances);

    public int[] GetClosestNeighbours(byte[] targetNodeId);
}