namespace Lantern.Discv5.WireProtocol.Table;

public interface IKBucket
{
    IEnumerable<NodeTableEntry> Nodes { get; }

    NodeTableEntry? GetNodeFromReplacementCache(byte[] nodeId, int bucketIndex);
    
    NodeTableEntry? GetNodeById(byte[] nodeId);
    
    Task Update(NodeTableEntry nodeEntry, int bucketIndex);
    
    void ReplaceDeadNode(NodeTableEntry nodeEntry, int bucketIndex);

    NodeTableEntry GetLeastRecentlySeenNode();

    void RefreshLeastRecentlySeenNode();
}