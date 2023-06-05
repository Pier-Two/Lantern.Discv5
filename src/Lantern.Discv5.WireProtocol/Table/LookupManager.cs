using System.Collections.Concurrent;
using Lantern.Discv5.WireProtocol.Packet;
using Microsoft.Extensions.Logging;

namespace Lantern.Discv5.WireProtocol.Table;

public class LookupManager : ILookupManager
{
    private readonly IRoutingTable _routingTable;
    private readonly IPacketManager _packetManager;
    private readonly TableOptions _tableOptions;
    private readonly ILogger<LookupManager> _logger;
    private readonly ConcurrentBag<PathBucket> _pathBuckets;

    public LookupManager(IRoutingTable routingTable, IPacketManager packetManager, ILoggerFactory loggerFactory, TableOptions tableOptions)
    {
        _routingTable = routingTable;
        _packetManager = packetManager;
        _logger = loggerFactory.CreateLogger<LookupManager>();
        _tableOptions = tableOptions;
        _pathBuckets = new ConcurrentBag<PathBucket>();
    }

    public bool IsLookupInProgress { get; private set; }

    public async Task<List<NodeTableEntry>?> LookupAsync(byte[] targetNodeId)
    {
        if (IsLookupInProgress)
        {
            _logger.LogInformation("Lookup is currently in progress");
            return null;
        }

        await Task.Delay(1000);
        IsLookupInProgress = true;
        await StartLookup(targetNodeId);

        var bucketCompletionTasks = _pathBuckets.Select(async bucket =>
        {
            var monitorTask = MonitorCompletionAsync(bucket);
            var completedTask = await Task.WhenAny(bucket.CompletionSource.Task, monitorTask);

            if (completedTask == bucket.CompletionSource.Task)
            {
                _logger.LogInformation("Bucket {BucketIndex} completed successfully", bucket.Index);
            }
            else
            {
                _logger.LogInformation("Bucket {BucketIndex} timed out", bucket.Index);
            }

            return completedTask == bucket.CompletionSource.Task;
        });

        await Task.WhenAll(bucketCompletionTasks);
    
        var completedBuckets = _pathBuckets.Where(bucket => bucket.IsComplete).ToList();
        var result = completedBuckets
            .SelectMany(bucket => bucket.DiscoveredNodes)
            .Distinct()
            .OrderBy(node => TableUtility.Log2Distance(node.Id, targetNodeId))
            .Take(TableConstants.BucketSize)
            .ToList();

        IsLookupInProgress = false;
        _pathBuckets.Clear();

        return result;
    }

    public async Task StartLookup(byte[] targetNodeId)
    {
        _logger.LogInformation("Starting lookup for target node {NodeID}", Convert.ToHexString(targetNodeId));

        var initialNodes = _routingTable.GetClosestNodes(targetNodeId)
            .Take(_tableOptions.ConcurrencyParameter)
            .ToList();
        
        _logger.LogInformation("Initial nodes count {InitialNodesCount}", initialNodes.Count);
        var pathBuckets = PartitionInitialNodesNew(initialNodes, targetNodeId);
        _logger.LogInformation("Total number of path buckets {PathBucketCount}", pathBuckets.Count);
        
        foreach (var pathBucket in pathBuckets)
        {
            _pathBuckets.Add(pathBucket);
            foreach (var node in pathBucket.Responses)
            {
                await QuerySelfNode(pathBucket, node.Key);
            }
        }
    }

    public async Task ContinueLookup(List<NodeTableEntry> nodes, byte[] senderNode, int expectedResponses)
    {
        foreach (var bucket in _pathBuckets.Where(bucket => bucket.Responses.Any(node => node.Key.SequenceEqual(senderNode))))
        {
            if (bucket.ExpectedResponses.ContainsKey(senderNode))
            {
                bucket.ExpectedResponses[senderNode]--;
            }
            else
            {
                bucket.ExpectedResponses.Add(senderNode, expectedResponses - 1);
            }
            
            _logger.LogInformation("Expecting {ExpectedResponses} more responses from node {NodeId} in QueryClosestNodes in bucket {BucketIndex}", bucket.ExpectedResponses[senderNode], Convert.ToHexString(senderNode), bucket.Index);
            _logger.LogInformation("Discovered {DiscoveredNodes} nodes so far in bucket {BucketIndex}", bucket.DiscoveredNodes.Count, bucket.Index);
            
            if (nodes.Count > 0)
            {
                _logger.LogDebug("Received {NodesCount} nodes from node {NodeId} in bucket {BucketIndex}", nodes.Count, Convert.ToHexString(senderNode), bucket.Index); 
                UpdatePathBucketNew(bucket, nodes, senderNode);
                await QueryClosestNodes(bucket, senderNode);
            }
            else
            {
                // If node replies with 0 nodes, vary the distance and try again
                _logger.LogDebug("Received no nodes from node {NodeId}. Varying distances", Convert.ToHexString(senderNode));
                await QuerySelfNode(bucket, senderNode);
            }
        }
    }

    private void UpdatePathBucketNew(PathBucket bucket, List<NodeTableEntry> nodes, byte[] senderNodeId)
    {
        var sortedNodes = nodes.OrderBy(nodeEntry => TableUtility.Log2Distance(nodeEntry.Id, bucket.TargetNodeId)).ToList();
        
        foreach (var node in sortedNodes)
        {
            bucket.Responses[senderNodeId].Add(node);
            bucket.DiscoveredNodes.Add(node);
            
            if (!bucket.Responses.ContainsKey(node.Id))
            {
                bucket.Responses.Add(node.Id, new List<NodeTableEntry>());
            }
        }

        bucket.Responses[senderNodeId].Sort((node1, node2) => TableUtility.Log2Distance(node1.Id, bucket.TargetNodeId).CompareTo(TableUtility.Log2Distance(node2.Id, bucket.TargetNodeId)));
        bucket.DiscoveredNodes.Sort((node1, node2) => TableUtility.Log2Distance(node1.Id, bucket.TargetNodeId).CompareTo(TableUtility.Log2Distance(node2.Id, bucket.TargetNodeId)));
    }

    private async Task QueryClosestNodes(PathBucket bucket, byte[] senderNodeId)
    {
        if (bucket.QueriedNodes.Count < TableConstants.BucketSize)
        {
            var nodesToQuery = bucket.Responses[senderNodeId].Take(_tableOptions.ConcurrencyParameter).ToList();
        
            if (bucket.ExpectedResponses[senderNodeId] != 0)
            {
                return;
            }

            bucket.ReceivedResponses++;
        
            foreach (var node in nodesToQuery)
            {
                _logger.LogDebug("Querying {NodesCount} nodes received from node {NodeId} in bucket {BucketIndex}", nodesToQuery.Count, Convert.ToHexString(senderNodeId), bucket.Index);

                if (_pathBuckets.Any(pathBucket => pathBucket.QueriedNodes.Contains(node.Id))) 
                    continue;
                
                bucket.QueriedNodes.Add(node.Id);
                await _packetManager.SendFindNodePacket(node.Record, bucket.TargetNodeId);
            }
        }
    }
    
    private async Task QuerySelfNode(PathBucket bucket, byte[] senderNodeId)
    {
        var node = _routingTable.GetNodeEntry(senderNodeId);
            
        if(node == null)
            return;

        if (bucket.ExpectedResponses[senderNodeId] != 0)
        {
            return;
        }
        
        bucket.ReceivedResponses++;

        if (bucket.QueriedNodes.Count < TableConstants.BucketSize)
        {
            if (!_pathBuckets.Any(pathBucket => pathBucket.QueriedNodes.Contains(node.Id)))
            {
                bucket.QueriedNodes.Add(node.Id);
                await _packetManager.SendFindNodePacket(node.Record, bucket.TargetNodeId);
            }
        }
    }
    
    private async Task MonitorCompletionAsync(PathBucket bucket)
    {
        while (!bucket.IsComplete)
        {
            if (bucket.ReceivedResponses == TableConstants.BucketSize || bucket.StartTime.ElapsedMilliseconds >= _tableOptions.LookupTimeoutMilliseconds)
            {
                bucket.IsComplete = true;
                bucket.CompletionSource.TrySetResult(true);
                _logger.LogInformation("Queried {QueriedNodes} nodes so far in bucket {BucketIndex}", bucket.QueriedNodes.Count, bucket.Index);
                _logger.LogInformation("Received {ReceivedResponses} responses so far in bucket {BucketIndex}", bucket.ReceivedResponses, bucket.Index);
            }
            else
            {
                await Task.Delay(100);
            }
        }
    }
    
    private List<PathBucket> PartitionInitialNodesNew(IReadOnlyList<NodeTableEntry> initialNodes, byte[] targetNodeId)
    {
        var bucketCount = Math.Min(initialNodes.Count, _tableOptions.LookupParallelism);
        var pathBuckets = new List<PathBucket>();

        for (var i = 0; i < bucketCount; i++)
        {
            pathBuckets.Add(new PathBucket(targetNodeId, i));
        }

        for (var i = 0; i < initialNodes.Count; i++)
        {
            pathBuckets[i % bucketCount].Responses.Add(initialNodes[i].Id, new List<NodeTableEntry>());
            pathBuckets[i % bucketCount].ExpectedResponses.Add(initialNodes[i].Id, 0);
        }

        return pathBuckets;
    }
}