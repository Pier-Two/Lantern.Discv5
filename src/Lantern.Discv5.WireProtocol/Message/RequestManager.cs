using Lantern.Discv5.WireProtocol.Connection;
using Lantern.Discv5.WireProtocol.Table;
using Lantern.Discv5.WireProtocol.Utility;
using Microsoft.Extensions.Logging;

namespace Lantern.Discv5.WireProtocol.Message;

public class RequestManager : IRequestManager
{
    private readonly Dictionary<byte[], PendingRequest> _pendingRequests;
    private readonly Dictionary<byte[], CachedRequest> _cachedRequests;
    private readonly IRoutingTable _routingTable;
    private readonly ILogger<RequestManager> _logger;
    private readonly TableOptions _tableOptions;
    private readonly ConnectionOptions _connectionOptions;
    private Task? _checkRequestsTask;
    private Task? _removeFulfilledRequestsTask;

    public RequestManager(IRoutingTable routingTable, ILoggerFactory loggerFactory, TableOptions tableOptions, ConnectionOptions connectionOptions)
    {
        _pendingRequests = new Dictionary<byte[], PendingRequest>(ByteArrayEqualityComparer.Instance);
        _cachedRequests = new Dictionary<byte[], CachedRequest>(ByteArrayEqualityComparer.Instance);
        _routingTable = routingTable;
        _logger = loggerFactory.CreateLogger<RequestManager>();
        _tableOptions = tableOptions;
        _connectionOptions = connectionOptions;
    }
    
    public void StartRequestManagerAsync(CancellationToken token = default)
    {
        _logger.LogInformation("Starting RequestManagerAsync");
        
        _checkRequestsTask = CheckRequestsAsync(token);
        _removeFulfilledRequestsTask = RemoveFulfilledRequestsAsync(token);
    }

    public async Task StopRequestManagerAsync(CancellationToken token = default)
    {
        _logger.LogInformation("Stopping RequestManagerAsync");

        try
        {
            if (_checkRequestsTask != null && _removeFulfilledRequestsTask != null)
            {
                await Task.WhenAll(_checkRequestsTask, _removeFulfilledRequestsTask).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            _logger.LogInformation("RequestManagerAsync was canceled gracefully");
        }
    }

    public bool AddPendingRequest(byte[] requestId, PendingRequest request)
    {
        if (ContainsPendingRequest(requestId)) 
            return false;
        
        _pendingRequests.Add(requestId, request);
        return true;
    }
    
    public bool AddCachedRequest(byte[] requestId, CachedRequest request)
    {
        if (ContainsCachedRequest(requestId)) 
            return false;
        
        _cachedRequests.Add(requestId, request);
        return true;
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
    
    public List<PendingRequest> GetPendingRequests()
    {
        return _pendingRequests.Values.ToList();
    }
    
    private List<CachedRequest> GetCachedRequests()
    {
        return _cachedRequests.Values.ToList();
    }

    public void MarkRequestAsFulfilled(byte[] requestId)
    {
        if (!ContainsPendingRequest(requestId)) 
            return;
        
        _pendingRequests[requestId].IsFulfilled = true;
        _pendingRequests[requestId].ResponsesReceived++;
    }

    public void MarkCachedRequestAsFulfilled(byte[] requestId)
    {
        if (ContainsCachedRequest(requestId))
        {
            _cachedRequests[requestId].IsFulfilled = true;
        }
    }

    private async Task CheckRequestsAsync(CancellationToken token)
    {
        _logger.LogInformation("Starting CheckPendingRequestsAsync");

        try
        {
            while (!token.IsCancellationRequested)
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

                await Task.Delay(_connectionOptions.RequestTimeoutMs, token).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            _logger.LogInformation("CheckPendingRequestsAsync was canceled gracefully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred in CheckPendingRequestsAsync");
        }

        _logger.LogInformation("CheckPendingRequestsAsync completed");
    }
    
    private async Task RemoveFulfilledRequestsAsync(CancellationToken token)
    {
        _logger.LogInformation("Starting RemoveCompletedTasksAsync");

        try
        {
            while (!token.IsCancellationRequested)
            {
                var completedTasks = GetPendingRequests().Where(x => x.IsFulfilled).ToList();
                
                foreach (var task in completedTasks)
                {
                    if (task.Message.MessageType == MessageType.FindNode)
                    {
                        if (task.ResponsesReceived == task.MaxResponses)
                        {
                            RemovePendingRequest(task.Message.RequestId);
                        }
                    }
                    else
                    {
                        RemovePendingRequest(task.Message.RequestId);
                    }
                }
                await Task.Delay(_connectionOptions.RemoveCompletedRequestsDelayMs, token).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
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
        if (request.ElapsedTime.ElapsedMilliseconds >= _connectionOptions.RequestTimeoutMs && !request.IsFulfilled)
        {
            _logger.LogDebug("Request timed out. Removing from pending requests");
            RemovePendingRequest(request.Message.RequestId);

            var nodeEntry = _routingTable.GetNodeEntry(request.NodeId);

            if (nodeEntry != null)
            {
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
        }
    }

    private void HandleCachedRequest(CachedRequest request)
    {
        if(request.ElapsedTime.ElapsedMilliseconds >= _connectionOptions.RequestTimeoutMs && !request.IsFulfilled)
        {
            _logger.LogInformation("Cached request timed out. Removing from cached requests");
            RemoveCachedRequest(request.NodeId);
            
            var nodeEntry = _routingTable.GetNodeEntry(request.NodeId);
            
            if (nodeEntry == null)
            {
                _logger.LogDebug("Node {NodeId} not found in routing table", Convert.ToHexString(request.NodeId));
                return;
            }
            
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
    }
    
    private void RemovePendingRequest(byte[] requestId)
    {
        if (ContainsPendingRequest(requestId))
        {
            _pendingRequests.Remove(requestId);
        }
    }

    private void RemoveCachedRequest(byte[] requestId)
    {
        if (ContainsCachedRequest(requestId))
        {
            _cachedRequests.Remove(requestId);
        }
    }
}

