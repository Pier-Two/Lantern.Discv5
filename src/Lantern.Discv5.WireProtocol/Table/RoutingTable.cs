using System.Collections.ObjectModel;
using Lantern.Discv5.Enr;
using Lantern.Discv5.WireProtocol.Identity;
using Lantern.Discv5.WireProtocol.Table;
using Microsoft.Extensions.Logging;

namespace Discv5ConsoleApp.Lantern.Discv5.WireProtocol.Table;

public class RoutingTable : IRoutingTable
{
    private readonly IIdentityManager _identityManager;
    private readonly IEnrRecordFactory _enrRecordFactory;
    private readonly ILogger<RoutingTable> _logger;
    private readonly List<KBucket> _buckets;

    public RoutingTable(IIdentityManager identityManager, IEnrRecordFactory enrRecordFactory, ILoggerFactory loggerFactory, TableOptions options)
    {
        _identityManager = identityManager;
        _enrRecordFactory = enrRecordFactory;
        _logger = loggerFactory.CreateLogger<RoutingTable>();
        _buckets = Enumerable.Range(0, TableConstants.NumberOfBuckets).Select(_ => new KBucket(loggerFactory, options.ReplacementCacheSize)).ToList();
        TableOptions = options;
    }

    public TableOptions TableOptions { get; }

    public int GetTotalEntriesCount()
    {
        lock (_buckets)
        {
            return _buckets.Sum(bucket => bucket.Nodes.Count() + bucket.ReplacementCache.Count());
        }
    }

    public int GetTotalActiveNodesCount()
    {
        lock (_buckets)
        {
            return _buckets.Sum(bucket => bucket.Nodes.Count(IsNodeConsideredLive) + bucket.ReplacementCache.Count(IsNodeConsideredLive));
        }
    }

    public NodeTableEntry[] GetAllNodeEntries()
    {
        lock (_buckets)
        {
            return _buckets.SelectMany(bucket => bucket.Nodes.Concat(bucket.ReplacementCache)).ToArray();
        }
    }
    
    public List<NodeTableEntry> GetClosestNodes(byte[] targetId)
    {
        lock (_buckets)
        {
            return _buckets
                .SelectMany(bucket => bucket.Nodes)
                .Where(nodeEntry => nodeEntry.HasRespondedEver)
                .OrderBy(nodeEntry => TableUtility.Log2Distance(nodeEntry.Id, targetId))
                .ToList(); 
        }
    }

    public void UpdateFromEnr(IEnrRecord enrRecord)
    {
        var nodeId = _identityManager.Verifier.GetNodeIdFromRecord(enrRecord);
        var nodeEntry = GetNodeEntry(nodeId) ?? new NodeTableEntry(enrRecord, _identityManager.Verifier);
        var bucketIndex = GetBucketIndex(nodeEntry.Id);

        _buckets[bucketIndex].Update(nodeEntry);
        _logger.LogDebug("Updated table with node entry {NodeId}", Convert.ToHexString(nodeId));
    }

    public NodeTableEntry? GetLeastRecentlySeenNode()
    {
        lock (_buckets)
        {
            var leastRecentlyRefreshedBucket = _buckets
                .Where(bucket => bucket.Nodes.Any())
                .MinBy(bucket => bucket.Nodes.Min(node => node.LastSeen));

            return leastRecentlyRefreshedBucket?.GetLeastRecentlySeenNode();
        }
    }

    public void MarkNodeAsResponded(byte[] nodeId)
    {
        var bucketIndex = GetBucketIndex(nodeId);
        var bucket = _buckets[bucketIndex];
        var nodeEntry = bucket.GetNodeById(nodeId);

        if (nodeEntry == null)
            return;

        nodeEntry.HasRespondedEver = true;
    }

    public void MarkNodeAsPending(byte[] nodeId)
    {
        var bucketIndex = GetBucketIndex(nodeId);
        var bucket = _buckets[bucketIndex];
        var nodeEntry = bucket.GetNodeById(nodeId);

        if (nodeEntry == null)
            return;

        nodeEntry.Status = NodeStatus.Pending;
    }

    public void MarkNodeAsLive(byte[] nodeId)
    {
        var bucketIndex = GetBucketIndex(nodeId);
        var bucket = _buckets[bucketIndex];
        var nodeEntry = bucket.GetNodeById(nodeId);

        if (nodeEntry == null)
            return;

        nodeEntry.Status = NodeStatus.Live;
        nodeEntry.FailureCounter = 0;
    }

    public void MarkNodeAsDead(byte[] nodeId)
    {
        var bucketIndex = GetBucketIndex(nodeId);
        var bucket = _buckets[bucketIndex];
        var nodeEntry = bucket.GetNodeById(nodeId);

        if (nodeEntry == null)
            return;

        nodeEntry.Status = NodeStatus.Dead;
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

        if (nodeEntry != null)
            return nodeEntry;

        var bootstrapEnrs = TableOptions.BootstrapEnrs
            .Select(enr => _enrRecordFactory.CreateFromString(enr, _identityManager.Verifier))
            .ToArray();

        foreach (var bootstrapEnr in bootstrapEnrs)
        {
            var bootstrapNodeId = _identityManager.Verifier.GetNodeIdFromRecord(bootstrapEnr);

            if (!nodeId.SequenceEqual(bootstrapNodeId))
                continue;

            var bootstrapEntry = new NodeTableEntry(bootstrapEnr, _identityManager.Verifier);
            return bootstrapEntry;
        }

        if (nodeEntry == null)
        {
            var bucketIndex = GetBucketIndex(nodeId);
            var bucket = _buckets[bucketIndex];
            nodeEntry = bucket.GetNodeFromReplacementCache(nodeId);
        }

        return nodeEntry;
    }

    public List<IEnrRecord> GetEnrRecordsAtDistances(IEnumerable<int> distances)
    {
        var enrRecords = new List<IEnrRecord>();

        foreach (var distance in distances)
        {
            if (distance == 0)
            {
                enrRecords.Add(_identityManager.Record);
            }
            else
            {
                var nodesAtDistance = GetNodesAtDistance(distance);
                
                if (nodesAtDistance == null)
                    continue;
                
                enrRecords.AddRange(nodesAtDistance.Select(nodeAtDistance => nodeAtDistance.Record));
            }
        }

        return enrRecords;
    }

    public void PopulateFromBootstrapEnrs()
    {
        var enrs = TableOptions.BootstrapEnrs
            .Select(enr => _enrRecordFactory.CreateFromString(enr, _identityManager.Verifier))
            .ToArray();

        foreach (var enr in enrs)
        {
            var nodeId = _identityManager.Verifier.GetNodeIdFromRecord(enr);
            var nodeEntry = GetNodeEntry(nodeId);

            if (nodeEntry == null)
                UpdateFromEnr(enr);
        }
    }

    private NodeTableEntry? GetEntryFromTable(byte[] nodeId)
    {
        var bucketIndex = GetBucketIndex(nodeId);
        var bucket = _buckets[bucketIndex];
        return bucket.GetNodeById(nodeId);
    }

    private bool IsNodeConsideredLive(NodeTableEntry nodeEntry)
    {
        return nodeEntry.Status == NodeStatus.Live && nodeEntry.FailureCounter < TableOptions.MaxAllowedFailures;
    }

    private List<NodeTableEntry>? GetNodesAtDistance(int distance)
    {
        if (distance is < 0 or > TableConstants.NumberOfBuckets)
        {
            _logger.LogError("Distance should be between 0 and 256");
            return null;
        }
        
        lock (_buckets)
        {
            var nodesAtDistance = new List<NodeTableEntry>();

            foreach (var bucket in _buckets)
            {
                foreach (var node in bucket.Nodes)
                {
                    if (!IsNodeConsideredLive(node))
                    {
                        continue;
                    }

                    var currentDistance = TableUtility.Log2Distance(_identityManager.Record.NodeId, node.Id);

                    if (currentDistance == distance)
                    {
                        nodesAtDistance.Add(node);
                    }
                }
            }

            return nodesAtDistance;
        }
    }

    private int GetBucketIndex(byte[] nodeId)
    {
        var distance = TableUtility.Log2Distance(_identityManager.Record.NodeId, nodeId);
        return distance == 256 ? 255 : distance;
    }
}