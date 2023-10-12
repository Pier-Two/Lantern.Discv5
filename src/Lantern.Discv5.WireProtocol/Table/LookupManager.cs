using System.Collections.Concurrent;
using Lantern.Discv5.WireProtocol.Connection;
using Lantern.Discv5.WireProtocol.Message;
using Lantern.Discv5.WireProtocol.Packet;
using Lantern.Discv5.WireProtocol.Utility;
using Microsoft.Extensions.Logging;

namespace Lantern.Discv5.WireProtocol.Table;

public class LookupManager(IRoutingTable routingTable,
        IPacketManager packetManager,
        IRequestManager requestManager,
        ILoggerFactory loggerFactory,
        ConnectionOptions connectionOptions,
        TableOptions tableOptions)
    : ILookupManager
{
    private readonly ILogger<LookupManager> _logger = loggerFactory.CreateLogger<LookupManager>();
    
    private readonly ConcurrentBag<PathBucket> _pathBuckets = new();
    
    private readonly SemaphoreSlim _lookupSemaphore = new(1, 1);

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
        var delayTask = Task.Delay(tableOptions.LookupTimeoutMilliseconds);

        await Task.WhenAny(allBucketsCompleteTask, delayTask);

        var result = _pathBuckets
            .SelectMany(bucket => bucket.DiscoveredNodes)
            .Distinct()
            .OrderBy(node => TableUtility.Log2Distance(node.Id, targetNodeId))
            .Take(TableConstants.BucketSize)
            .ToList();
        
        PrintLookupSummary();
        
        foreach (var bucket in _pathBuckets)
        {
            bucket.Dispose();
        }

        IsLookupInProgress = false;

        return result;
    }

    public async Task StartLookupAsync(byte[] targetNodeId)
    {
        _logger.LogInformation("Starting lookup for target node {NodeID}", Convert.ToHexString(targetNodeId));

        var initialNodes = routingTable.GetClosestNodes(targetNodeId)
            .Take(tableOptions.ConcurrencyParameter)
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
        try
        {
            foreach (var bucket in _pathBuckets)
            {
                if (!bucket.PendingQueries.Contains(senderNodeId, ByteArrayEqualityComparer.Instance) || bucket.Completion.Task.IsCompleted)
                    continue;

                if (bucket.ExpectedResponses.TryGetValue(senderNodeId, out var value))
                    bucket.ExpectedResponses[senderNodeId] = value - 1;
                else
                    bucket.ExpectedResponses.TryAdd(senderNodeId, expectedResponses - 1);

                _logger.LogDebug(
                    "Expecting {ExpectedResponses} more responses from node {NodeId} in QueryClosestNodes in bucket {BucketIndex}",
                    bucket.ExpectedResponses[senderNodeId], Convert.ToHexString(senderNodeId), bucket.Index);

                UpdatePathBucket(bucket, nodes, senderNodeId);

                if (bucket.ExpectedResponses[senderNodeId] != 0)
                    return;
                
                if (bucket.ExpectedResponses.Count >= TableConstants.BucketSize)
                {
                    _logger.LogInformation("Marking bucket {BucketIndex} as complete. Received responses from {Count} closest nodes", bucket.Index,bucket.ExpectedResponses.Count);
                    bucket.SetComplete();
                }
                else
                    await QueryClosestNodes(bucket, senderNodeId);
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error in ContinueLookupAsync");
        }
    }

    private static void UpdatePathBucket(PathBucket bucket, List<NodeTableEntry> nodes, byte[] senderNodeId)
    {
        var sortedNodes = nodes.OrderBy(nodeEntry => TableUtility.Log2Distance(nodeEntry.Id, bucket.TargetNodeId))
            .ToList();

        foreach (var node in sortedNodes)
        {
            bucket.Responses[senderNodeId].Add(node);
            bucket.DiscoveredNodes.Add(node);

            if (!bucket.Responses.ContainsKey(node.Id))
            {
                bucket.Responses.TryAdd(node.Id, new List<NodeTableEntry>());
            }
        }

        bucket.Responses[senderNodeId].Sort((node1, node2) => TableUtility.Log2Distance(node1.Id, bucket.TargetNodeId)
            .CompareTo(TableUtility.Log2Distance(node2.Id, bucket.TargetNodeId)));
        
        bucket.DiscoveredNodes.ToList().Sort((node1, node2) => TableUtility.Log2Distance(node1.Id, bucket.TargetNodeId)
                .CompareTo(TableUtility.Log2Distance(node2.Id, bucket.TargetNodeId)));
    }

    private async Task QuerySelfNode(PathBucket bucket, byte[] senderNodeId)
    {
        var node = routingTable.GetNodeEntry(senderNodeId);

        if (node == null)
            return;

        _logger.LogInformation("Querying self node {NodeId} in bucket {BucketIndex}", Convert.ToHexString(node.Id),
            bucket.Index);
            
        bucket.PendingTimers[node.Id] = new Timer(_ => QueryTimeoutCallback(node.Id, bucket), null,
            connectionOptions.ReceiveTimeoutMs, connectionOptions.ReceiveTimeoutMs);
        bucket.PendingQueries.Add(node.Id);
        
        await packetManager.SendPacket(node.Record, MessageType.FindNode, bucket.TargetNodeId);
    }

    private async Task QueryClosestNodes(PathBucket bucket, byte[] senderNodeId)
    {
        var pendingQueries = TableConstants.BucketSize - bucket.ExpectedResponses.Count;

        if (pendingQueries <= 0) 
            return; 
        
        var nodesToQueryCount = Math.Min(pendingQueries, tableOptions.ConcurrencyParameter);
        
        if (nodesToQueryCount == 0)
            return;
        
        var nodesToQuery = bucket.Responses[senderNodeId]
            .Where(node => routingTable.GetNodeEntry(node.Id) != null)
            .Where(node => !_pathBuckets.Any(pathBucket => pathBucket.ExpectedResponses.ContainsKey(node.Id)))
            .Where(node => !requestManager.ContainsCachedRequest(node.Id))
            .Take(nodesToQueryCount)
            .ToList();
        
        if(nodesToQuery.Count == 0)
            return;
        
        _logger.LogInformation("Querying {NodesCount} nodes received from node {NodeId} in bucket {BucketIndex}",
            nodesToQuery.Count, Convert.ToHexString(senderNodeId), bucket.Index);

        foreach (var node in nodesToQuery)
        {
            if (bucket.ExpectedResponses.Count >= TableConstants.BucketSize)
                return;
            
            bucket.PendingTimers[node.Id] =
                new Timer(_ => QueryTimeoutCallback(node.Id, bucket), null, connectionOptions.ReceiveTimeoutMs, connectionOptions.ReceiveTimeoutMs);
            bucket.PendingQueries.Add(node.Id);
            await packetManager.SendPacket(node.Record, MessageType.FindNode, bucket.TargetNodeId);
        }
    }

    private async Task QueryTimeoutCallback(byte[] nodeId, PathBucket bucket)
    {
        try
        {
            await _lookupSemaphore.WaitAsync();
            
            bucket.DisposeTimer(nodeId);
            
            if (bucket.Completion.Task.IsCompleted || (bucket.Responses.ContainsKey(nodeId) && bucket.Responses[nodeId].Count > 0))
            {
                _lookupSemaphore.Release();
                return;
            }

            var replacementNode = bucket.DiscoveredNodes
                .Where(node => routingTable.GetNodeEntry(node.Id) != null)
                .Where(node => !_pathBuckets.Any(pathBucket => pathBucket.ExpectedResponses.ContainsKey(node.Id)))
                .FirstOrDefault(node => !requestManager.ContainsCachedRequest(node.Id));

            if (replacementNode == null)
            {
                _lookupSemaphore.Release();
                _logger.LogInformation("No replacement node found in bucket {BucketIndex}", bucket.Index);
                return;
            }
            
            bucket.PendingTimers[replacementNode.Id] = new Timer(_ => QueryTimeoutCallback(replacementNode.Id, bucket),
                null, connectionOptions.RequestTimeoutMs,connectionOptions.ReceiveTimeoutMs);
            bucket.PendingQueries.Add(replacementNode.Id);
            
            _logger.LogInformation("Querying a replaced node {NodeId} in bucket {BucketIndex}",
                Convert.ToHexString(replacementNode.Id), bucket.Index);
            
            await packetManager.SendPacket(replacementNode.Record, MessageType.FindNode, bucket.TargetNodeId);
            _lookupSemaphore.Release();
        }
        catch (Exception e)
        {
            _lookupSemaphore.Release();
            _logger.LogError(e, "Error in QueryTimeoutCallback");
        }
    }

    private List<PathBucket> PartitionInitialNodesNew(IReadOnlyList<NodeTableEntry> initialNodes, byte[] targetNodeId)
    {
        var bucketCount = Math.Min(initialNodes.Count, tableOptions.LookupParallelism);
        var pathBuckets = new List<PathBucket>();

        for (var i = 0; i < bucketCount; i++)
        {
            pathBuckets.Add(new PathBucket(targetNodeId, i));
            pathBuckets[i].Responses.TryAdd(initialNodes[i].Id, new List<NodeTableEntry>());
        }

        return pathBuckets;
    }
    
    private void PrintLookupSummary()
    {
        foreach (var bucket in _pathBuckets)
        {
            if (bucket.Completion.Task.IsCompleted)
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