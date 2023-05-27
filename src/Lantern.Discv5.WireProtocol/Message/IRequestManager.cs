namespace Lantern.Discv5.WireProtocol.Message;

public interface IRequestManager
{
    Task StartRequestManagerAsync(CancellationToken token = default);
    
    Task StopRequestManagerAsync(CancellationToken token = default);
    
    bool AddPendingRequest(byte[] requestId, PendingRequest request);
    
    bool ContainsPendingRequest(byte[] requestId);
    
    PendingRequest? GetPendingRequest(byte[] requestId);
    
    List<PendingRequest> GetPendingRequests();

    void MarkRequestAsFulfilled(byte[] requestId);
}