using System.Collections.Concurrent;
using Lantern.Discv5.WireProtocol.Message;
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
        
        IsLookupInProgress = true;
        
        await Task.Delay(1000);
        await StartLookupAsync(targetNodeId);
        
        var allBucketsCompleteTask = Task.WhenAll(_pathBuckets.Select(bucket => bucket.Completion.Task));
        var delayTask = Task.Delay(_tableOptions.LookupTimeoutMilliseconds);
        
        await Task.WhenAny(allBucketsCompleteTask, delayTask);
        PrintLookupSummary();
        
        var result = _pathBuckets
            .SelectMany(bucket => bucket.DiscoveredNodes)
            .Distinct()
            .OrderBy(node => TableUtility.Log2Distance(node.Id, targetNodeId))
            .Take(TableConstants.BucketSize)
            .ToList();

        IsLookupInProgress = false;
        _pathBuckets.Clear();

        return result;
    }


    public async Task StartLookupAsync(byte[] targetNodeId)
    {
        _logger.LogInformation("Starting lookup for target node {NodeID}", Convert.ToHexString(targetNodeId));

        var initialNodes = _routingTable.GetClosestNodes(targetNodeId)
            .Take(_tableOptions.ConcurrencyParameter)
            .ToList();
        
        _logger.LogDebug("Initial nodes count {InitialNodesCount}", initialNodes.Count);
        var pathBuckets = PartitionInitialNodesNew(initialNodes, targetNodeId);
        _logger.LogDebug("Total number of path buckets {PathBucketCount}", pathBuckets.Count);
        
        foreach (var pathBucket in pathBuckets)
        {
            _pathBuckets.Add(pathBucket);
            
            foreach (var node in pathBucket.Responses)
            {
                await QuerySelfNode(pathBucket, node.Key);
            }
        }
    }

    public async Task ContinueLookupAsync(List<NodeTableEntry> nodes, byte[] senderNodeId, int expectedResponses)
    {
        foreach (var bucket in _pathBuckets.Where(bucket => !bucket.IsComplete && bucket.Responses.Any(node => node.Key.SequenceEqual(senderNodeId))))
        {
            if (bucket.ExpectedResponses.ContainsKey(senderNodeId))
            {
                bucket.ExpectedResponses[senderNodeId]--;
            }
            else
            {
                bucket.ExpectedResponses.Add(senderNodeId, expectedResponses - 1);
            }
            
            _logger.LogInformation("Expecting {ExpectedResponses} more responses from node {NodeId} in QueryClosestNodes in bucket {BucketIndex}", bucket.ExpectedResponses[senderNodeId], Convert.ToHexString(senderNodeId), bucket.Index);
            _logger.LogDebug("Discovered {DiscoveredNodes} nodes so far in bucket {BucketIndex}", bucket.DiscoveredNodes.Count, bucket.Index);
            _logger.LogInformation("Received {NodesCount} nodes from node {NodeId} in bucket {BucketIndex}", nodes.Count, Convert.ToHexString(senderNodeId), bucket.Index);
            _logger.LogInformation("Received responses from {ReceivedResponsesCount} nodes so far in bucket {BucketIndex}", bucket.ExpectedResponses.Count, bucket.Index);
            
            UpdatePathBucket(bucket, nodes, senderNodeId);
            
            if (bucket.ExpectedResponses[senderNodeId] != 0)
            {
                return;
            }

            if (nodes.Count > 0)
            {
                await QueryClosestNodes(bucket, senderNodeId);
            }
            else
            {
                await QuerySelfNode(bucket, senderNodeId);
            }
        }
    }

    private void UpdatePathBucket(PathBucket bucket, List<NodeTableEntry> nodes, byte[] senderNodeId)
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

        if (bucket.ExpectedResponses.Count >= TableConstants.BucketSize)
        {
            bucket.SetComplete();
        }

        bucket.Responses[senderNodeId].Sort((node1, node2) => TableUtility.Log2Distance(node1.Id, bucket.TargetNodeId).CompareTo(TableUtility.Log2Distance(node2.Id, bucket.TargetNodeId)));
        bucket.DiscoveredNodes.Sort((node1, node2) => TableUtility.Log2Distance(node1.Id, bucket.TargetNodeId).CompareTo(TableUtility.Log2Distance(node2.Id, bucket.TargetNodeId)));
    }
    
    private async Task QuerySelfNode(PathBucket bucket, byte[] senderNodeId)
    {
        var node = _routingTable.GetNodeEntry(senderNodeId);
            
        if(node == null)
            return;
        
        if (!_pathBuckets.Any(pathBucket => pathBucket.QueriedNodes.Contains(node.Id)))
        {
            if (bucket.ExpectedResponses.Count >= TableConstants.BucketSize) 
                return;
            
            _logger.LogDebug("Querying self node {NodeId} in bucket {BucketIndex}", Convert.ToHexString(node.Id), bucket.Index);
                
            bucket.QueriedNodes.Add(node.Id);
            await _packetManager.SendPacket(node.Record, MessageType.FindNode,bucket.TargetNodeId);
        }
    }

    private async Task QueryClosestNodes(PathBucket bucket, byte[] senderNodeId)
    {
        var nodesToQuery = bucket.Responses[senderNodeId].Take(_tableOptions.ConcurrencyParameter).ToList();
            
        _logger.LogDebug("Querying {NodesCount} nodes received from node {NodeId} in bucket {BucketIndex}", nodesToQuery.Count, Convert.ToHexString(senderNodeId), bucket.Index);

        foreach (var node in nodesToQuery)
        {
            if (bucket.ExpectedResponses.Count >= TableConstants.BucketSize) 
                return;
            
            if (_pathBuckets.Any(pathBucket => pathBucket.QueriedNodes.Contains(node.Id))) 
                continue;

            bucket.QueriedNodes.Add(node.Id);
            await _packetManager.SendPacket(node.Record, MessageType.FindNode, bucket.TargetNodeId);
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

    private void PrintLookupSummary()
    {
        foreach (var bucket in _pathBuckets)
        {
            if (bucket.IsComplete)
            {
                _logger.LogInformation("Bucket {BucketIndex} is complete", bucket.Index);
            }
            else
            {
                _logger.LogInformation("Bucket {BucketIndex} is incomplete", bucket.Index);
            }
        }
    }
}