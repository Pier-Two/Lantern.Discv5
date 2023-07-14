using System.Collections.Concurrent;
using Lantern.Discv5.WireProtocol.Connection;
using Lantern.Discv5.WireProtocol.Table;
using Lantern.Discv5.WireProtocol.Utility;
using Microsoft.Extensions.Logging;

namespace Lantern.Discv5.WireProtocol.Message;

public class RequestManager : IRequestManager
{
    private readonly ConcurrentDictionary<byte[], PendingRequest> _pendingRequests;
    private readonly ConcurrentDictionary<byte[], byte[]> _cachedHandshakeInteractions;
    private readonly ConcurrentDictionary<byte[], CachedRequest> _cachedRequests;
    private readonly IRoutingTable _routingTable;
    private readonly ILogger<RequestManager> _logger;
    private readonly TableOptions _tableOptions;
    private readonly ConnectionOptions _connectionOptions;
    private readonly CancellationTokenSource _shutdownCts;
    private Timer? _checkRequestsTimer;
    private Timer? _removeFulfilledRequestsTimer;

    public RequestManager(IRoutingTable routingTable, ILoggerFactory loggerFactory, TableOptions tableOptions, ConnectionOptions connectionOptions)
    {
        _pendingRequests = new ConcurrentDictionary<byte[], PendingRequest>(ByteArrayEqualityComparer.Instance);
        _cachedHandshakeInteractions = new ConcurrentDictionary<byte[], byte[]>(ByteArrayEqualityComparer.Instance);
        _cachedRequests = new ConcurrentDictionary<byte[], CachedRequest>(ByteArrayEqualityComparer.Instance);
        _routingTable = routingTable;
        _logger = loggerFactory.CreateLogger<RequestManager>();
        _tableOptions = tableOptions;
        _connectionOptions = connectionOptions;
        _shutdownCts = new CancellationTokenSource();
    }
    
    public void StartRequestManager()
    {
        _logger.LogInformation("Starting RequestManagerAsync");
        
        _checkRequestsTimer = new Timer(CheckRequests, null, 0, _connectionOptions.CheckPendingRequestsDelayMs);
        _removeFulfilledRequestsTimer = new Timer(RemoveFulfilledRequests, null, 0, _connectionOptions.RemoveCompletedRequestsDelayMs);
    }

    public async Task StopRequestManagerAsync()
    {
        _logger.LogInformation("Stopping RequestManagerAsync");
        _shutdownCts.Cancel();

        if (_checkRequestsTimer != null)
        {
            await _checkRequestsTimer.DisposeAsync();
            _checkRequestsTimer = null; 
        }

        if (_removeFulfilledRequestsTimer != null)
        {
            await _removeFulfilledRequestsTimer.DisposeAsync();
            _removeFulfilledRequestsTimer = null; 
        }
    }

    public bool AddPendingRequest(byte[] requestId, PendingRequest request)
    {
        var result = _pendingRequests.ContainsKey(requestId);
        _pendingRequests.AddOrUpdate(requestId, request, (_, _) => request);

        if (!result)
        {
            _routingTable.MarkNodeAsPending(request.NodeId);
            _logger.LogDebug("Added pending request with id {RequestId}", Convert.ToHexString(requestId));
        }

        return !result;
    }

    public bool AddCachedRequest(byte[] requestId, CachedRequest request)
    {
        var result = _cachedRequests.ContainsKey(requestId);
        _cachedRequests.AddOrUpdate(requestId, request, (_, _) => request);

        if (!result)
        {
            _routingTable.MarkNodeAsPending(request.NodeId);
            _logger.LogDebug("Added cached request with id {RequestId}", Convert.ToHexString(requestId));
        }

        return !result;
    }
    
    public void AddCachedHandshakeInteraction(byte[] packetNonce, byte[] destNodeId)
    { 
        _cachedHandshakeInteractions.TryAdd(packetNonce, destNodeId);
    }
    
    public byte[]? GetCachedHandshakeInteraction(byte[] packetNonce)
    {
        return _cachedHandshakeInteractions.TryRemove(packetNonce, out var destNodeId) ? destNodeId : null;
    }

    public bool ContainsCachedRequest(byte[] requestId)
    {
        return _cachedRequests.ContainsKey(requestId);
    }
    
    public PendingRequest? GetPendingRequest(byte[] requestId)
    {
        _pendingRequests.TryGetValue(requestId, out var request);
        return request;
    }
    
    public PendingRequest? GetPendingRequestByNodeId(byte[] nodeId)
    {
        return _pendingRequests.Values.FirstOrDefault(x => x.NodeId.SequenceEqual(nodeId));
    }

    public CachedRequest? GetCachedRequest(byte[] requestId)
    {
        _cachedRequests.TryGetValue(requestId, out var request);
        return request;
    }

    public PendingRequest? MarkRequestAsFulfilled(byte[] requestId)
    {
        if (!_pendingRequests.TryGetValue(requestId, out var request)) 
            return null;
        
        request.IsFulfilled = true;
        request.ResponsesCount++;
        
        return request;
    }

    public CachedRequest? MarkCachedRequestAsFulfilled(byte[] requestId)
    {
        _logger.LogDebug("Marking cached request as fulfilled with id {RequestId}", Convert.ToHexString(requestId));
        _cachedRequests.TryRemove(requestId, out var request);

        return request;
    }

    // CheckRequests and RemoveFulfilledRequests can likely be merged into one method
    private void CheckRequests(object? state)
    {
        _logger.LogDebug("Checking for pending and cached requests");

        var currentPendingRequests = _pendingRequests.Values.ToList();
        var currentCachedRequests = _cachedRequests.Values.ToList();

        foreach (var pendingRequest in currentPendingRequests)
        {
            HandlePendingRequest(pendingRequest);
        }
        
        foreach (var cachedRequest in currentCachedRequests)
        {
            HandleCachedRequest(cachedRequest);
        }
    }
    
    private void RemoveFulfilledRequests(object? state)
    {
        _logger.LogDebug("Removing fulfilled requests");

        var completedTasks = _pendingRequests.Values.ToList().Where(x => x.IsFulfilled).ToList();

        foreach (var task in completedTasks)
        {
            if (task.Message.MessageType == MessageType.FindNode)
            {
                if (task.ResponsesCount == task.MaxResponses)
                {
                    _pendingRequests.TryRemove(task.Message.RequestId, out _);
                }
            }
            else
            {
                _pendingRequests.TryRemove(task.Message.RequestId, out _);
            }
        }
    }

    private void HandlePendingRequest(PendingRequest request)
    {
        if (request.ElapsedTime.ElapsedMilliseconds <= _connectionOptions.RequestTimeoutMs) 
            return;
        
        _logger.LogDebug("Pending request timed out for node {NodeId}", Convert.ToHexString(request.NodeId));

        _pendingRequests.TryRemove(request.Message.RequestId, out _);
        var nodeEntry = _routingTable.GetNodeEntry(request.NodeId);

        if (nodeEntry == null) 
            return;
        
        if(nodeEntry.FailureCounter >= _tableOptions.MaxAllowedFailures)
        {
            _logger.LogDebug("Node {NodeId} has reached max retries. Marking as dead", Convert.ToHexString(request.NodeId));
            _routingTable.MarkNodeAsDead(request.NodeId);
        }
        else
        {
            _logger.LogDebug("Increasing failure counter for Node {NodeId}",Convert.ToHexString(request.NodeId));
            _routingTable.IncreaseFailureCounter(request.NodeId);
        }
    }

    private void HandleCachedRequest(CachedRequest request)
    {
        if (request.ElapsedTime.ElapsedMilliseconds <= _connectionOptions.RequestTimeoutMs) 
            return;
        
        _logger.LogDebug("Cached request timed out for node {NodeId}", Convert.ToHexString(request.NodeId));
        
        _cachedRequests.TryRemove(request.NodeId, out _);  
        var nodeEntry = _routingTable.GetNodeEntry(request.NodeId);
            
        if (nodeEntry == null)
        {
            _logger.LogDebug("Node {NodeId} not found in routing table", Convert.ToHexString(request.NodeId));
            return;
        }

        if (!nodeEntry.HasRespondedEver)
        {
            _routingTable.MarkNodeAsDead(request.NodeId);
        }
    }
}