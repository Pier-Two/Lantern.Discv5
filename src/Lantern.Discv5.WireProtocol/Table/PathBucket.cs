using System.Diagnostics;
using Lantern.Discv5.WireProtocol.Utility;

namespace Lantern.Discv5.WireProtocol.Table;

public class PathBucket
{
    public int Index { get; }
    
    public byte[] TargetNodeId { get; }

    public List<byte[]> QueriedNodes { get; }
    
    public List<NodeTableEntry> DiscoveredNodes { get; }

    public Dictionary<byte[], List<NodeTableEntry>> Responses { get; }
    
    public Dictionary<byte[], int> ExpectedResponses { get; }
    
    public TaskCompletionSource<bool> CompletionSource { get; }
    
    public int ReceivedResponses { get; set; }

    public bool IsComplete { get; set; }
    
    public Stopwatch StartTime = Stopwatch.StartNew();

    public PathBucket(byte[] targetNodeId, int index) 
    {
        Index = index;
        TargetNodeId = targetNodeId;
        QueriedNodes = new List<byte[]>();
        DiscoveredNodes = new List<NodeTableEntry>();
        Responses = new Dictionary<byte[], List<NodeTableEntry>>(ByteArrayEqualityComparer.Instance);
        CompletionSource = new TaskCompletionSource<bool>();
        ExpectedResponses = new Dictionary<byte[], int>();
    }
}