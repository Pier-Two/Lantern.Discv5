using Lantern.Discv5.Enr;
using Lantern.Discv5.WireProtocol.Identity;
using Microsoft.Extensions.Logging;

namespace Lantern.Discv5.WireProtocol.Table;

public class RoutingTable : IRoutingTable
{
    private readonly IIdentityManager _identityManager;
    private readonly ILogger<RoutingTable> _logger;
    private readonly byte[] _localNodeId;
    private readonly List<KBucket> _buckets;
    private readonly TableOptions _options;
    
    public RoutingTable(IIdentityManager identityManager, ILoggerFactory loggerFactory, TableOptions options)
    {
        _identityManager = identityManager;
        _localNodeId = identityManager.NodeId;
        _options = options;
        _logger = loggerFactory.CreateLogger<RoutingTable>();
        _buckets = Enumerable.Range(0, TableConstants.NumberOfBuckets).Select(_ => new KBucket(loggerFactory,options, TableConstants.BucketSize)).ToList();
    }
    
    public IEnumerable<EnrRecord> GetBootstrapEnrs() => _options.BootstrapEnrs;
    
    public int GetTotalEntriesCount() => _buckets.Sum(bucket => bucket.Nodes.Count());

    public int GetTotalActiveNodesCount() => _buckets.Sum(bucket => bucket.Nodes.Count(IsNodeConsideredLive));

    public NodeTableEntry[] GetAllNodeEntries() => _buckets.SelectMany(bucket => bucket.Nodes).ToArray();
    
    public void UpdateTable(EnrRecord enrRecord)
    {
        var nodeId = _identityManager.Verifier.GetNodeIdFromRecord(enrRecord);
        var nodeEntry = GetNodeEntry(nodeId) ?? new NodeTableEntry(enrRecord, _identityManager.Verifier);
        var bucketIndex = GetBucketIndex(nodeEntry.Id);

        _buckets[bucketIndex].Update(nodeEntry, bucketIndex);
        _logger.LogInformation("Updated table with node entry {NodeId}", Convert.ToHexString(nodeId));
    }

    public void RefreshBuckets()
    {
        if (GetTotalEntriesCount() <= 0) 
            return;
        
        _logger.LogInformation("Refreshing buckets...");
        
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

        if (nodeEntry == null) 
            return;
        
        nodeEntry.IsLive = true;
        nodeEntry.FailureCounter = 0;
    }
    
    public void MarkNodeAsDead(byte[] nodeId)
    {
        var bucketIndex = GetBucketIndex(nodeId);
        var bucket = _buckets[bucketIndex];
        var nodeEntry = bucket.GetNodeById(nodeId);

        if (nodeEntry == null) 
            return;
        
        nodeEntry.IsLive = false;
        bucket.ReplaceDeadNode(nodeEntry, bucketIndex);
    }

    public void IncreaseFailureCounter(byte[] nodeId)
    {
        var bucketIndex = GetBucketIndex(nodeId);
        var bucket = _buckets[bucketIndex];
        var nodeEntry = bucket.GetNodeById(nodeId);
        
        if (nodeEntry != null)
        {
            nodeEntry.FailureCounter++;
        }
    }

    public NodeTableEntry? GetNodeEntry(byte[] nodeId)
    {
        var nodeEntry = GetEntryFromTable(nodeId);

        if (nodeEntry == null)
        {
            var bootstrapEnrs = GetBootstrapEnrs();

            foreach (var bootstrapEnr in bootstrapEnrs)
            {
                var bootstrapNodeId = _identityManager.Verifier.GetNodeIdFromRecord(bootstrapEnr);

                if (!nodeId.SequenceEqual(bootstrapNodeId)) 
                    continue;
                
                var bootstrapEntry = new NodeTableEntry(bootstrapEnr, _identityManager.Verifier);
                return bootstrapEntry;
            }
        }
        else
        {
            return nodeEntry;
        }
        
        return null;
    }

    public List<EnrRecord> GetEnrRecordsAtDistances(IEnumerable<int> distances)
    {
        var enrRecords = new List<EnrRecord>();

        foreach (var distance in distances)
        {
            if (distance == 0)
            {
                enrRecords.Add(_identityManager.Record);
            }
            else
            {
                var nodesAtDistance = GetNodesAtDistance(distance);
                enrRecords.AddRange(nodesAtDistance.Select(nodeAtDistance => nodeAtDistance.Record));
            }
        }

        return enrRecords;
    }

    public List<NodeTableEntry> GetClosestNodes(byte[] targetId)
    { 
        return _buckets
            .SelectMany(bucket => bucket.Nodes)
            .Where(IsNodeConsideredLive)
            .OrderBy(nodeEntry => TableUtility.Log2Distance(nodeEntry.Id, targetId))
            .ToList();
    }

    private NodeTableEntry? GetEntryFromTable(byte[] nodeId)
    {
        var bucketIndex = GetBucketIndex(nodeId);
        var bucket = _buckets[bucketIndex];
        return bucket.GetNodeById(nodeId);
    }
    
    public bool IsNodeConsideredLive(NodeTableEntry nodeEntry)
    {
        // A node is considered live if it is marked as live or if its LivenessCounter is greater than the maximum allowed failures
        return nodeEntry.IsLive && nodeEntry.FailureCounter < _options.MaxAllowedFailures;
    }
    
    private List<NodeTableEntry> GetNodesAtDistance(int distance)
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
    
    private int GetBucketIndex(byte[] nodeId)
    {
        var distance = TableUtility.Log2Distance(_localNodeId, nodeId);
        
        return distance == 256 ? 255 : distance;
    }
}