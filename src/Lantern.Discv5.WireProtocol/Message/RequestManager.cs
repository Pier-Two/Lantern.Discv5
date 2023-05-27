using Lantern.Discv5.WireProtocol.Connection;
using Lantern.Discv5.WireProtocol.Table;
using Lantern.Discv5.WireProtocol.Utility;
using Microsoft.Extensions.Logging;

namespace Lantern.Discv5.WireProtocol.Message;

public class RequestManager : IRequestManager
{
    private readonly Dictionary<byte[], PendingRequest> _pendingRequests;
    private readonly IRoutingTable _routingTable;
    private readonly ILogger<RequestManager> _logger;
    private readonly CancellationTokenSource _shutdownCts;
    private readonly TableOptions _tableOptions;
    private readonly ConnectionOptions _connectionOptions;
    private Task? _removeCompletedTasks;
    private Task? _pendingRequestsTask;

    public RequestManager(IRoutingTable routingTable, ILoggerFactory loggerFactory, TableOptions tableOptions, ConnectionOptions connectionOptions)
    {
        _pendingRequests = new Dictionary<byte[], PendingRequest>(ByteArrayEqualityComparer.Instance);
        _routingTable = routingTable;
        _logger = loggerFactory.CreateLogger<RequestManager>();
        _tableOptions = tableOptions;
        _connectionOptions = connectionOptions;
        _shutdownCts = new CancellationTokenSource();
    }
    
    public async Task StartRequestManagerAsync(CancellationToken token = default)
    {
        _logger.LogInformation("Starting RequestManagerAsync");
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(token, _shutdownCts.Token);

        try
        {
            _pendingRequestsTask = CheckPendingRequestsAsync(linkedCts.Token);
            _removeCompletedTasks = RemoveCompletedTasksAsync(linkedCts.Token);
            
            await Task.WhenAll(_pendingRequestsTask, _removeCompletedTasks).ConfigureAwait(false);
        }
        finally
        {
            linkedCts.Dispose();
        }
    }
    
    public async Task StopRequestManagerAsync(CancellationToken token = default)
    {
        _logger.LogInformation("Stopping RequestManagerAsync");
        _shutdownCts.Cancel();
        
        try
        {
            if (_pendingRequestsTask != null && _removeCompletedTasks != null)
            {
                await Task.WhenAll(_pendingRequestsTask, _removeCompletedTasks).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested || _shutdownCts.IsCancellationRequested)
        {
            _logger.LogInformation("RequestManagerAsync was canceled gracefully");
        }
    }

    public bool AddPendingRequest(byte[] requestId, PendingRequest request)
    {
        if (ContainsPendingRequest(requestId) == false)
        {
            _pendingRequests.Add(requestId, request);
            return true;
        }
        
        return false;
    }
    
    public bool ContainsPendingRequest(byte[] requestId)
    {
        return _pendingRequests.ContainsKey(requestId);
    }
    
    public PendingRequest? GetPendingRequest(byte[] requestId)
    {
        _pendingRequests.TryGetValue(requestId, out var request);
        return request;
    }
    
    public List<PendingRequest> GetPendingRequests()
    {
        return _pendingRequests.Values.ToList();
    }
    
    public void MarkRequestAsFulfilled(byte[] requestId)
    {
        if (ContainsPendingRequest(requestId))
        {
            _pendingRequests[requestId].IsFulfilled = true;
            _pendingRequests[requestId].ResponsesReceived++;
        }
    }

    private async Task CheckPendingRequestsAsync(CancellationToken token)
    {
        _logger.LogInformation("Starting CheckPendingRequestsAsync");

        try
        {
            while (!token.IsCancellationRequested)
            {
                var currentRequests = GetPendingRequests();

                foreach (var request in currentRequests)
                {
                    HandlePendingRequest(request);
                }

                await Task.Delay(_connectionOptions.CheckPendingRequestsDelayMs, token).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested || _shutdownCts.IsCancellationRequested)
        {
            _logger.LogInformation("CheckPendingRequestsAsync was canceled gracefully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred in CheckPendingRequestsAsync");
        }

        _logger.LogInformation("CheckPendingRequestsAsync completed");
    }
    
    private async Task RemoveCompletedTasksAsync(CancellationToken token)
    {
        _logger.LogInformation("Starting RemoveCompletedTasksAsync");

        try
        {
            while (!token.IsCancellationRequested)
            {
                await Task.Delay(_connectionOptions.RemoveCompletedRequestsDelayMs, token).ConfigureAwait(false);
                var completedTasks = GetPendingRequests().Where(x => x.IsFulfilled).ToList();
                
                foreach (var task in completedTasks)
                {
                    RemovePendingRequest(task.Message.RequestId);
                }
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested || _shutdownCts.IsCancellationRequested)
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
            _logger.LogInformation("Request timed out. Removing from pending requests");
            RemovePendingRequest(request.Message.RequestId);

            var nodeEntry = _routingTable.GetNodeEntry(request.NodeId);

            if (nodeEntry != null)
            {
                if(nodeEntry.FailureCounter >= _tableOptions.MaxAllowedFailures)
                {
                    _logger.LogInformation("Node {NodeId} has reached max retries. Marking as dead", Convert.ToHexString(request.NodeId));
                    _routingTable.MarkNodeAsDead(request.NodeId);
                }
                else
                {
                    _logger.LogInformation("Increasing failure counter for Node {NodeId}",Convert.ToHexString(request.NodeId));
                    _routingTable.IncreaseFailureCounter(request.NodeId);
                }
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
}

