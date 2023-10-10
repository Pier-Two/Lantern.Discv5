using System.Collections.Concurrent;
using Lantern.Discv5.WireProtocol.Utility;

namespace Lantern.Discv5.WireProtocol.Table;

public class PathBucket(byte[] targetNodeId, int index)
{
    public int Index { get; } = index;

    public byte[] TargetNodeId { get; } = targetNodeId;

    public ConcurrentDictionary<byte[], Timer> PendingTimers { get; } = new(ByteArrayEqualityComparer.Instance);

    public ConcurrentBag<NodeTableEntry> DiscoveredNodes { get; } = new();

    public ConcurrentDictionary<byte[], List<NodeTableEntry>> Responses { get; } = new(ByteArrayEqualityComparer.Instance);

    public ConcurrentDictionary<byte[], int> ExpectedResponses { get; } = new();

    public TaskCompletionSource<bool> Completion { get; } = new();

    // Method of setting completion status
    public void SetComplete()
    {
        if (Completion.Task.IsCompleted) 
            return;
        
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