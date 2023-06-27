using System.Collections.Concurrent;
using Lantern.Discv5.WireProtocol.Connection;
using Lantern.Discv5.WireProtocol.Table;
using Lantern.Discv5.WireProtocol.Utility;
using Microsoft.Extensions.Logging;

namespace Lantern.Discv5.WireProtocol.Message;

public class RequestManager : IRequestManager
{
    private readonly ConcurrentDictionary<byte[], PendingRequest> _pendingRequests;
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
        _logger.LogDebug("Adding pending request with id {RequestId}", Convert.ToHexString(requestId));
        return _pendingRequests.TryAdd(requestId, request);
    }
    
    public bool AddCachedRequest(byte[] requestId, CachedRequest request)
    {
        _logger.LogDebug("Adding cached request with id {RequestId}", Convert.ToHexString(requestId));
        return _cachedRequests.TryAdd(requestId, request);
    }

    public bool ContainsPendingRequest(byte[] requestId)
    {
        return _pendingRequests.ContainsKey(requestId);
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

    public CachedRequest? GetCachedRequest(byte[] requestId)
    {
        _cachedRequests.TryGetValue(requestId, out var request);
        return request;
    }
    
    public void MarkRequestAsFulfilled(byte[] requestId)
    {
        if (!_pendingRequests.TryGetValue(requestId, out var request)) 
            return;
        
        request.IsFulfilled = true;
        request.ResponsesCount++;
    }

    public void MarkCachedRequestAsFulfilled(byte[] requestId)
    {
        _cachedRequests.TryRemove(requestId, out _);
    }
    
    private List<PendingRequest> GetPendingRequests()
    {
        return _pendingRequests.Values.ToList();
    }
    
    private List<CachedRequest> GetCachedRequests()
    {
        return _cachedRequests.Values.ToList();
    }

    private void CheckRequests(object? state)
    {
        _logger.LogInformation("Starting CheckPendingRequestsAsync");

        try
        {
            var currentPendingRequests = GetPendingRequests();
            var currentCachedRequests = GetCachedRequests();

            foreach (var pendingRequest in currentPendingRequests)
            {
                HandlePendingRequest(pendingRequest);
            }

            foreach (var cachedRequest in currentCachedRequests)
            {
                HandleCachedRequest(cachedRequest);
            }

        }
        catch (OperationCanceledException) when (_shutdownCts.IsCancellationRequested)
        {
            _logger.LogInformation("CheckPendingRequestsAsync was canceled gracefully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred in CheckPendingRequestsAsync");
        }

        _logger.LogInformation("CheckPendingRequestsAsync completed");
    }
    
    private void RemoveFulfilledRequests(object? state)
    {
        _logger.LogInformation("Starting RemoveCompletedTasksAsync");

        try
        {
            var completedTasks = GetPendingRequests().Where(x => x.IsFulfilled).ToList();

            foreach (var task in completedTasks)
            {
                if (task.Message.MessageType == MessageType.FindNode)
                {
                    if (task.ResponsesCount == task.MaxResponses)
                    {
                        RemovePendingRequest(task.Message.RequestId);
                    }
                }
                else
                {
                    RemovePendingRequest(task.Message.RequestId);
                }
            }
        }
        catch (OperationCanceledException) when (_shutdownCts.IsCancellationRequested)
        {
            _logger.LogInformation("RemoveCompletedTasksAsync was canceled gracefully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred in RemoveCompletedTasksAsync");
        }

        _logger.LogInformation("RemoveCompletedTasksAsync completed");
    }

    private void HandlePendingRequest(PendingRequest request)
    {
        if (request.ElapsedTime.ElapsedMilliseconds < _connectionOptions.PendingRequestTimeoutMs ||
            request.IsFulfilled) 
            return;
        
        _logger.LogDebug("Request timed out for node {NodeId}. Removing from pending requests", Convert.ToHexString(request.NodeId));
        RemovePendingRequest(request.Message.RequestId);

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
        if (request.ElapsedTime.ElapsedMilliseconds < _connectionOptions.PendingRequestTimeoutMs ||
            request.IsFulfilled) 
            return;
        
        _logger.LogDebug("Cached request timed out for node {NodeId}. Removing from cached requests", Convert.ToHexString(request.NodeId));
        RemoveCachedRequest(request.NodeId);
            
        var nodeEntry = _routingTable.GetNodeEntry(request.NodeId);
            
        if (nodeEntry == null)
        {
            _logger.LogDebug("Node {NodeId} not found in routing table", Convert.ToHexString(request.NodeId));
            return;
        }
            
        _routingTable.MarkNodeAsDead(request.NodeId);
    }
    
    private void RemovePendingRequest(byte[] requestId)
    {
        _pendingRequests.TryRemove(requestId, out _);
    }

    private void RemoveCachedRequest(byte[] requestId)
    {
        _cachedRequests.TryRemove(requestId, out _);
    }
}

