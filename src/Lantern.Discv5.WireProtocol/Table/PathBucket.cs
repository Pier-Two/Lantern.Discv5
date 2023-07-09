using System.Collections.Concurrent;
using System.Diagnostics;
using Lantern.Discv5.WireProtocol.Utility;

namespace Lantern.Discv5.WireProtocol.Table;

public class PathBucket
{
    public int Index { get; }
    
    public byte[] TargetNodeId { get; }
    
    public ConcurrentDictionary<byte[], TaskCompletionSource<bool>> PendingQueries { get; }

    public ConcurrentBag<byte[]> QueriedNodes { get; }
    
    public List<NodeTableEntry> DiscoveredNodes { get; }

    public Dictionary<byte[], List<NodeTableEntry>> Responses { get; }
    
    public Dictionary<byte[], int> ExpectedResponses { get; }
    
    public bool IsComplete { get; private set; }
    
    public TaskCompletionSource<bool> Completion { get; } = new();

    public PathBucket(byte[] targetNodeId, int index) 
    {
        Index = index;
        TargetNodeId = targetNodeId;
        PendingQueries = new ConcurrentDictionary<byte[], TaskCompletionSource<bool>>(ByteArrayEqualityComparer.Instance);
        QueriedNodes = new ConcurrentBag<byte[]>();
        DiscoveredNodes = new List<NodeTableEntry>();
        Responses = new Dictionary<byte[], List<NodeTableEntry>>(ByteArrayEqualityComparer.Instance);
        ExpectedResponses = new Dictionary<byte[], int>();
    }
    
    // Method of setting completion status
    public void SetComplete()
    {
        IsComplete = true;
        Completion.SetResult(true);  
    }
    
    public ConcurrentDictionary<byte[], Timer> PendingTimers { get; } = new(ByteArrayEqualityComparer.Instance);

    public byte[]? DisposeTimer(byte[] nodeId)
    {
        if (PendingTimers.TryRemove(nodeId, out var timer))
        {
            timer.Dispose();
            return nodeId;
        }
        return null;
    }
}