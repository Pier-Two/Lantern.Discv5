using Lantern.Discv5.WireProtocol.Packet;
using Microsoft.Extensions.Logging;

namespace Lantern.Discv5.WireProtocol.Table;

public class LookupManager : ILookupManager
{
    private readonly IRoutingTable _routingTable;
    private readonly IPacketManager _packetManager;
    private readonly TableOptions _tableOptions;
    private readonly ILogger<LookupManager> _logger;
    private readonly List<PathBucket> _pathBuckets;

    public LookupManager(IRoutingTable routingTable, IPacketManager packetManager, ILoggerFactory loggerFactory, TableOptions tableOptions)
    {
        _routingTable = routingTable;
        _packetManager = packetManager;
        _tableOptions = tableOptions;
        _logger = loggerFactory.CreateLogger<LookupManager>();
        _pathBuckets = new List<PathBucket>();
    }

    public async Task PerformLookup(byte[] targetNodeId)
    {
        _logger.LogInformation("Starting lookup for target node {NodeID}", Convert.ToHexString(targetNodeId));

        var initialNodes = _routingTable.GetClosestNodes(targetNodeId)
            .Take(_tableOptions.ConcurrencyParameter)
            .ToList();
        
        _logger.LogInformation("Initial nodes count {InitialNodesCount}", initialNodes.Count);
        
        var pathBuckets = PartitionInitialNodes(initialNodes, targetNodeId);
        _pathBuckets.AddRange(pathBuckets);
        _logger.LogInformation("Total number of path buckets {PathBucketCount}", pathBuckets.Count);
        
        foreach (var pathBucket in _pathBuckets)
        {
            await QueryNodesInBucket(pathBucket);
        }
    }
    
    public PathBucket GetBucketByNodeId(byte[] nodeId)
    {
        return _pathBuckets.First(bucket => bucket.TargetNodeId.SequenceEqual(nodeId));
    }

    public List<PathBucket> GetCompletedBuckets()
    {
        return _pathBuckets.Where(bucket => bucket.IsComplete).ToList();
    }
    
    public async Task ContinueLookup(List<NodeTableEntry> nodes, byte[] senderNode, int expectedResponses)
    {
        foreach (var bucket in _pathBuckets.Where(bucket => bucket.QueriedNodes.Any(node => node.Id.SequenceEqual(senderNode))))
        {
            await UpdatePathBucket(bucket, nodes, senderNode, expectedResponses);
            return;
        }
    }
    
    private async Task QueryNodesInBucket(PathBucket bucket)
    {
        var nodesToQuery = bucket.NodesToQuery.Take(_tableOptions.ConcurrencyParameter).ToList();
        
        _logger.LogInformation("Nodes to query count {NodesCount} in path bucket {Index}", nodesToQuery.Count, bucket.Index);
        
        foreach (var node in nodesToQuery)
        {
            await _packetManager.SendFindNodePacket(node.Record, bucket.TargetNodeId);
            
            bucket.NodesToQuery.Remove(node);
            bucket.QueriedNodes.Add(node);
        }
        
        _logger.LogInformation("Queried node count {QueriedNodeCount} in path bucket {Index}", bucket.QueriedNodes.Count, bucket.Index);
    }
    
    private async Task UpdatePathBucket(PathBucket bucket, List<NodeTableEntry> nodes, byte[] senderNode, int expectedResponses)
    {
        _logger.LogInformation("Received {NodesCount} nodes from node {NodeId} in bucket {BucketIndex}", nodes.Count, Convert.ToHexString(senderNode), bucket.Index);
        var sortedNodes = nodes.OrderBy(nodeEntry => TableUtility.Log2Distance(nodeEntry.Id, bucket.TargetNodeId)).ToList(); 
            
        foreach (var newNode in sortedNodes.Where(newNode => !bucket.DiscoveredNodes.Any(existingNode => existingNode.Id.SequenceEqual(newNode.Id))))
        {
            bucket.DiscoveredNodes.Add(newNode);
        }
        
        
        bucket.DiscoveredNodes.Sort((node1, node2) => TableUtility.Log2Distance(node1.Id, bucket.TargetNodeId).CompareTo(TableUtility.Log2Distance(node2.Id, bucket.TargetNodeId)));
        bucket.NodesToQuery.AddRange(bucket.DiscoveredNodes);
            
        _logger.LogInformation("Discovered {DiscoveredNodesCount} nodes from node {NodeId} in bucket {BucketIndex}", bucket.DiscoveredNodes.Count, Convert.ToHexString(senderNode), bucket.Index);

        if (bucket.ExpectedResponses.ContainsKey(senderNode))
        {
            bucket.ExpectedResponses[senderNode]--;
        }
        else
        {
            bucket.ExpectedResponses[senderNode] = expectedResponses - 1;
        }
        
        _logger.LogInformation("Expected count of responses {ExpectedResponsesCount} from node {NodeId} in bucket {BucketIndex}", bucket.ExpectedResponses[senderNode], Convert.ToHexString(senderNode), bucket.Index);

        if (bucket.QueriedNodes.Count < TableConstants.BucketSize)
        {
            if(bucket.ExpectedResponses[senderNode] == 0)
            {
                _logger.LogInformation("No more responses expected. Continuing lookup for path bucket {Index} for target node {TargetNodeId}", bucket.Index, Convert.ToHexString(bucket.TargetNodeId));
                await QueryNodesInBucket(bucket);
            }
        }
        else
        {
            _logger.LogInformation("Lookup completed for path bucket {Index} with target node {TargetNodeId}", bucket.Index, Convert.ToHexString(bucket.TargetNodeId));
            bucket.IsComplete = true;
            
            foreach (var node in bucket.DiscoveredNodes)
            {
                _logger.LogInformation("Found {ClosestNodeId} closest to target node {TargetNodeId}", Convert.ToHexString(node.Id), Convert.ToHexString(bucket.TargetNodeId));
            }
        }
    }

    private List<PathBucket> PartitionInitialNodes(IReadOnlyList<NodeTableEntry> initialNodes, byte[] targetNodeId)
    {
        var bucketCount = Math.Min(initialNodes.Count, _tableOptions.LookupParallelism);
        var pathBuckets = new List<PathBucket>();

        for (var i = 0; i < bucketCount; i++)
        {
            pathBuckets.Add(new PathBucket(targetNodeId, i));
        }

        for (var i = 0; i < initialNodes.Count; i++)
        {
            pathBuckets[i % bucketCount].NodesToQuery.Add(initialNodes[i]);
        }

        return pathBuckets;
    }
}