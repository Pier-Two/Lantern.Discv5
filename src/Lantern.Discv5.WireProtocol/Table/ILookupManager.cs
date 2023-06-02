namespace Lantern.Discv5.WireProtocol.Table;

public interface ILookupManager
{
    Task<List<NodeTableEntry>> LookupAsync(byte[] targetNodeId);
    
    Task StartLookup(byte[] targetNodeId);
    
    Task ContinueLookup(List<NodeTableEntry> nodes, byte[] senderNode, int expectedResponses);

    PathBucket GetBucketByNodeId(byte[] nodeId);

    List<PathBucket> GetCompletedBuckets();

    bool IsLookupInProgress { get; }
}