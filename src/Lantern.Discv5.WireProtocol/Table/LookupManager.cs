using System.Collections.Concurrent;
using Lantern.Discv5.WireProtocol.Connection;
using Lantern.Discv5.WireProtocol.Message;
using Lantern.Discv5.WireProtocol.Packet;
using Microsoft.Extensions.Logging;

namespace Lantern.Discv5.WireProtocol.Table;

public class LookupManager : ILookupManager
{
    private readonly IRoutingTable _routingTable;
    private readonly IPacketManager _packetManager;
    private readonly ConnectionOptions _connectionOptions;
    private readonly TableOptions _tableOptions;
    private readonly IRequestManager _requestManager;
    private readonly ILogger<LookupManager> _logger;
    private readonly ConcurrentBag<PathBucket> _pathBuckets;
    private readonly SemaphoreSlim _lookupSemaphore = new(1, 1);

    public LookupManager(IRoutingTable routingTable, IPacketManager packetManager, IRequestManager requestManager, ILoggerFactory loggerFactory, ConnectionOptions connectionOptions, TableOptions tableOptions)
    {
        _routingTable = routingTable;
        _packetManager = packetManager;
        _requestManager = requestManager;
        _logger = loggerFactory.CreateLogger<LookupManager>();
        _connectionOptions = connectionOptions;
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
        
        await MonitorLookupAsync(allBucketsCompleteTask, delayTask);
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
            
            if(bucket.PendingQueries.ContainsKey(senderNodeId))
            {
                if (!bucket.PendingQueries[senderNodeId].Task.IsCompleted)
                {
                    bucket.PendingQueries[senderNodeId].SetResult(true);
                    await QueryTimeoutCallback(senderNodeId, bucket); // Stop timeout timer
                }
            }
            
            _logger.LogDebug("Expecting {ExpectedResponses} more responses from node {NodeId} in QueryClosestNodes in bucket {BucketIndex}", bucket.ExpectedResponses[senderNodeId], Convert.ToHexString(senderNodeId), bucket.Index);
            _logger.LogDebug("Discovered {DiscoveredNodes} nodes so far in bucket {BucketIndex}", bucket.DiscoveredNodes.Count, bucket.Index);
            _logger.LogDebug("Received {NodesCount} nodes from node {NodeId} in bucket {BucketIndex}", nodes.Count, Convert.ToHexString(senderNodeId), bucket.Index);
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

    private async Task MonitorLookupAsync(Task allBucketsCompleteTask, Task delayTask)
    {
        while (true)
        {
            var completedTask = await Task.WhenAny(allBucketsCompleteTask, delayTask);

            if (completedTask == allBucketsCompleteTask)
            {
                _logger.LogInformation("All buckets are complete. Stopping lookup");
                break;
            }

            if (completedTask == delayTask)
            {
                _logger.LogInformation("Lookup timed out. Stopping lookup");
                break; 
            }
            
            PrintLookupSummary();
        }
    }

    private static void UpdatePathBucket(PathBucket bucket, List<NodeTableEntry> nodes, byte[] senderNodeId)
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
        
        if (bucket.ExpectedResponses.Count >= TableConstants.BucketSize)
        {
            bucket.SetComplete();
        }
    }
    
    private async Task QuerySelfNode(PathBucket bucket, byte[] senderNodeId)
    {
        var node = _routingTable.GetNodeEntry(senderNodeId);
        
        if(node == null)
            return;
        
        if (!_pathBuckets.Any(pathBucket => pathBucket.QueriedNodes.Contains(node.Id)) && !_pathBuckets.Any(pathBucket => pathBucket.PendingQueries.ContainsKey(node.Id)))
        {
            if (bucket.ExpectedResponses.Count >= TableConstants.BucketSize) 
                return;
            
            _logger.LogInformation("Querying self node {NodeId} in bucket {BucketIndex}", Convert.ToHexString(node.Id), bucket.Index);
            
            bucket.QueriedNodes.Add(node.Id);
            bucket.PendingQueries.TryAdd(node.Id, new TaskCompletionSource<bool>());
            bucket.PendingTimers[node.Id] = new Timer(_ => QueryTimeoutCallback(node.Id, bucket), null, _connectionOptions.ReceiveTimeoutMs, Timeout.Infinite);
            
            await _packetManager.SendPacket(node.Record, MessageType.FindNode,bucket.TargetNodeId);
        }
    }
    
    private async Task QueryTimeoutCallback(byte[] nodeId, PathBucket bucket)
    {
        try
        {
            bucket.DisposeTimer(nodeId);
            await _lookupSemaphore.WaitAsync();

            var unqueriedNode = bucket.DiscoveredNodes
                .Where(node => !_pathBuckets.Any(pathBucket => pathBucket.QueriedNodes.Contains(node.Id)))
                .Where(node => !_pathBuckets.Any(pathBucket => pathBucket.PendingQueries.ContainsKey(node.Id)))
                .Where(node => !_pathBuckets.Any(pathBucket => pathBucket.ExpectedResponses.ContainsKey(node.Id)))
                .FirstOrDefault(node => !_requestManager.ContainsCachedRequest(node.Id));
        
            if(unqueriedNode == null)
            {
                _lookupSemaphore.Release();
                return;
            }

            bucket.QueriedNodes.Add(unqueriedNode.Id);
            bucket.PendingQueries.TryAdd(unqueriedNode.Id, new TaskCompletionSource<bool>());
            bucket.PendingTimers[unqueriedNode.Id] = new Timer(_ => QueryTimeoutCallback(unqueriedNode.Id, bucket), null, _connectionOptions.RequestTimeoutMs, Timeout.Infinite);
        
            _logger.LogInformation("Querying a replaced node {NodeId} in bucket {BucketIndex}", Convert.ToHexString(unqueriedNode.Id), bucket.Index);
            await _packetManager.SendPacket(unqueriedNode.Record, MessageType.FindNode, bucket.TargetNodeId);

            _lookupSemaphore.Release();
        }
        catch (Exception e)
        {
            _lookupSemaphore.Release();
            _logger.LogError(e, "Error in QueryTimeoutCallback");
        }
    }

    private async Task QueryClosestNodes(PathBucket bucket, byte[] senderNodeId)
    {
        var nodesToQuery = bucket.Responses[senderNodeId].Take(_tableOptions.ConcurrencyParameter).ToList();
            
        _logger.LogInformation("Querying {NodesCount} nodes received from node {NodeId} in bucket {BucketIndex}", nodesToQuery.Count, Convert.ToHexString(senderNodeId), bucket.Index);

        foreach (var node in nodesToQuery)
        {
            if (bucket.ExpectedResponses.Count >= TableConstants.BucketSize) 
                return;
            
            if (_pathBuckets.Any(pathBucket => pathBucket.QueriedNodes.Contains(node.Id)) && _pathBuckets.Any(pathBucket => pathBucket.PendingQueries.ContainsKey(node.Id)))
                continue;

            bucket.QueriedNodes.Add(node.Id);
            bucket.PendingQueries.TryAdd(node.Id, new TaskCompletionSource<bool>());
            bucket.PendingTimers[node.Id] = new Timer(_ => QueryTimeoutCallback(node.Id, bucket), null, 1000, Timeout.Infinite);
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
                _logger.LogInformation("Bucket {BucketIndex} timed out", bucket.Index);
            }
        }
    }
}