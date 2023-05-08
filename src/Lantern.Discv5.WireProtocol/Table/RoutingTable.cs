namespace Lantern.Discv5.WireProtocol.Table;

public class RoutingTable
{
    private readonly byte[] _localNodeId;
    private readonly List<KBucket> _buckets;
    private readonly TableOptions _options;

    public RoutingTable(TableOptions options, byte[] localNodeId)
    {
        _localNodeId = localNodeId;
        _options = options;
        _buckets = Enumerable.Range(0, TableConstants.NumberOfBuckets)
            .Select(_ => new KBucket(options, TableConstants.BucketSize))
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
        _buckets[bucketIndex].Update(nodeEntry, bucketIndex);
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

    public void MarkNodeAsQueried(byte[] nodeId)
    {
        var bucketIndex = GetBucketIndex(nodeId);
        var bucket = _buckets[bucketIndex];
        var nodeEntry = bucket.GetNodeById(nodeId);
        
        if (nodeEntry != null)
        {
            nodeEntry.IsQueried = true;
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
    
    public List<NodeTableEntry> GetNodesAtDistance(int distance)
    {
        if (distance is < 0 or > TableConstants.NumberOfBuckets)
        {
            throw new ArgumentOutOfRangeException(nameof(distance), "Distance should be between 0 and 256.");
        }

        var nodesAtDistance = new List<NodeTableEntry>();

        foreach (var bucket in _buckets)
        {
            foreach (var node in bucket.Nodes)
            {
                if (!IsNodeConsideredLive(node))
                {
                    continue;
                }

                var currentDistance = TableUtility.Log2Distance(_localNodeId, node.Id);

                if (currentDistance == distance)
                {
                    nodesAtDistance.Add(node);
                }
            }
        }

        return nodesAtDistance;
    }

    public int GetTotalEntriesCount()
    {
        return _buckets.Sum(bucket => bucket.Nodes.Count());
    }
    
    public List<NodeTableEntry> GetInitialNodesForLookup(byte[] targetId)
    {
        return GetClosestNodes(targetId, _options.LookupConcurrency);
    }

    private List<NodeTableEntry> GetClosestNodes(byte[] targetId, int k) // Added k parameter
    {
        return _buckets
            .SelectMany(bucket => bucket.Nodes)
            .Where(IsNodeConsideredLive)
            .OrderBy(nodeEntry => TableUtility.Log2Distance(nodeEntry.Id, targetId))
            .Take(k)
            .ToList();
    }

    private int GetBucketIndex(byte[] nodeId)
    {
        var distance = TableUtility.Log2Distance(_localNodeId, nodeId);
        return (int)Math.Floor(Math.Log(distance, 2));
    }
    
    private bool IsNodeConsideredLive(NodeTableEntry nodeEntry)
    {
        // A node is considered live if it is marked as live or if its LivenessCounter is greater than the maximum allowed failures
        return nodeEntry.IsLive || nodeEntry.LivenessCounter > _options.MaxAllowedFailures;
    }
}