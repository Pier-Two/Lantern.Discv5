namespace Lantern.Discv5.WireProtocol.Table;

public class RoutingTable
{
    private const int MaxAllowedFailures = 3; 
    private const int NodeIdSize = 256;
    private const int BucketSize = 16;
    private const int NumberOfBuckets = 256;
    private readonly byte[] _localNodeId;
    private readonly List<KBucket> _buckets;

    public RoutingTable(byte[] localNodeId, int maxReplacementCacheSize = 3)
    {
        _localNodeId = localNodeId;
        _buckets = Enumerable.Range(0, NumberOfBuckets)
            .Select(_ => new KBucket(BucketSize, maxReplacementCacheSize))
            .ToList();
    }
    
    public NodeTableEntry? GetNodeEntry(byte[] nodeId)
    {
        var bucketIndex = GetBucketIndex(nodeId);
        var bucket = _buckets[bucketIndex];
        return bucket.GetNodeById(nodeId);
    }

    public void Update(NodeTableEntry nodeEntry)
    {
        var bucketIndex = GetBucketIndex(nodeEntry.Id);
        _buckets[bucketIndex].Update(nodeEntry);
    }

    public NodeTableEntry GetNodeForFindNode(byte[] targetId)
    {
        var bucketIndex = GetBucketIndex(targetId);
        return _buckets[bucketIndex].GetLeastRecentlySeenNode();
    }

    public void RefreshBuckets()
    {
        foreach (var bucket in _buckets)
        {
            bucket.RefreshLeastRecentlySeenNode();
        }
    }
    
    public void MarkNodeAsLive(byte[] nodeId)
    {
        var bucketIndex = GetBucketIndex(nodeId);
        var bucket = _buckets[bucketIndex];
        var nodeEntry = bucket.GetNodeById(nodeId);
        
        if (nodeEntry != null)
        {
            nodeEntry.IsLive = true;
        }
    }
    
    public NodeTableEntry GetNodeEntryAtDistance(int distance)
    {
        if (distance is < 0 or >= NumberOfBuckets)
        {
            throw new ArgumentOutOfRangeException(nameof(distance), "Distance should be between 0 and 255.");
        }

        var bucket = _buckets[distance];
        var nodeEntry = GetMostRecentlySeenNode(bucket);

        if (nodeEntry != null && IsNodeConsideredLive(nodeEntry))
        {
            return nodeEntry;
        }

        return null;
    }

    public int GetTotalNodeCount()
    {
        return _buckets.Sum(bucket => bucket.Nodes.Count());
    }
    
    public List<NodeTableEntry> GetInitialNodesForLookup(byte[] targetId, int alpha = 3)
    {
        return GetClosestNodes(targetId, alpha);
    }

    private List<NodeTableEntry> GetClosestNodes(byte[] targetId, int k) // Added k parameter
    {
        return _buckets
            .SelectMany(bucket => bucket.Nodes)
            .Where(IsNodeConsideredLive)
            .OrderBy(nodeEntry => Log2Distance(nodeEntry.Id, targetId))
            .Take(k)
            .ToList();
    }

    private int GetBucketIndex(byte[] nodeId)
    {
        var distance = Log2Distance(_localNodeId, nodeId);
        return (int)Math.Floor(Math.Log(distance, 2));
    }
    
    public static int Log2Distance(ReadOnlySpan<byte> firstNodeId, ReadOnlySpan<byte> secondNodeId)
    {
        var firstMatch = 0;
        var logDistance = 0;

        for (var i = 0; i < firstNodeId.Length; i++)
        {
            var xoredByte = (byte)(firstNodeId[i] ^ secondNodeId[i]);

            if (xoredByte != 0)
            {
                while ((xoredByte & 0x80) == 0)
                {
                    xoredByte <<= 1;
                    firstMatch++;
                }
                break;
            }

            firstMatch += 8;
        }

        logDistance = NodeIdSize - firstMatch;
        return logDistance;
    }
    
    private static bool IsNodeConsideredLive(NodeTableEntry nodeEntry)
    {
        // A node is considered live if it is marked as live or if its LivenessCounter is greater than the maximum allowed failures
        return nodeEntry.IsLive || nodeEntry.LivenessCounter > MaxAllowedFailures;
    }
    
    private static NodeTableEntry? GetMostRecentlySeenNode(KBucket bucket)
    {
        return bucket.Nodes.Any() ? bucket.Nodes.Last() : null;
    }
}