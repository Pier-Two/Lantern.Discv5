using System.Collections.Concurrent;
using System.Diagnostics;
using Lantern.Discv5.WireProtocol.Utility;

namespace Lantern.Discv5.WireProtocol.Table;

public class PathBucket
{
    public int Index { get; }
    
    public byte[] TargetNodeId { get; }
    
    public ConcurrentDictionary<byte[], TaskCompletionSource<bool>> PendingQueries { get; }
    
    public ConcurrentDictionary<byte[], Timer> PendingTimers { get; } = new(ByteArrayEqualityComparer.Instance);

    public ConcurrentBag<NodeTableEntry> DiscoveredNodes { get; }

    public ConcurrentDictionary<byte[], List<NodeTableEntry>> Responses { get; }
    
    public ConcurrentDictionary<byte[], int> ExpectedResponses { get; }
    
    public bool IsComplete { get; private set; }
    
    public TaskCompletionSource<bool> Completion { get; } = new();

    public PathBucket(byte[] targetNodeId, int index) 
    {
        Index = index;
        TargetNodeId = targetNodeId;
        PendingQueries = new ConcurrentDictionary<byte[], TaskCompletionSource<bool>>(ByteArrayEqualityComparer.Instance);
        DiscoveredNodes = new ConcurrentBag<NodeTableEntry>();
        Responses = new ConcurrentDictionary<byte[], List<NodeTableEntry>>(ByteArrayEqualityComparer.Instance);
        ExpectedResponses = new ConcurrentDictionary<byte[], int>();
    }
    
    // Method of setting completion status
    public void SetComplete()
    {
        if (Completion.Task.IsCompleted) 
            return;
        
        IsComplete = true;
        Completion.SetResult(true);
    }
    
    public void DisposeTimer(byte[] nodeId)
    {
        if (PendingTimers.TryRemove(nodeId, out var timer))
        {
            timer.Dispose();
        }
    }
}