namespace Lantern.Discv5.WireProtocol.Table;

public class PathBucket
{
    public int Index { get; }
    
    public byte[] TargetNodeId { get; }
    
    public List<NodeTableEntry> NodesToQuery { get; }
    
    public List<NodeTableEntry> QueriedNodes { get; }
    
    public List<NodeTableEntry> DiscoveredNodes { get; }
    
    public Dictionary<byte[], int> ExpectedResponses { get; }

    public bool IsComplete { get; set; }

    public PathBucket(byte[] targetNodeId, int index) 
    {
        Index = index;
        TargetNodeId = targetNodeId;
        NodesToQuery = new List<NodeTableEntry>();
        QueriedNodes = new List<NodeTableEntry>();
        DiscoveredNodes = new List<NodeTableEntry>();
        ExpectedResponses = new Dictionary<byte[], int>();
    }
}