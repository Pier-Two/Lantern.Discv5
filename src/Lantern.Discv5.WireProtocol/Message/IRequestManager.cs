namespace Lantern.Discv5.WireProtocol.Message;

public interface IRequestManager
{
    void StartRequestManagerAsync(CancellationToken token = default);
    
    Task StopRequestManagerAsync(CancellationToken token = default);
    
    bool AddPendingRequest(byte[] requestId, PendingRequest request);

    bool AddCachedRequest(byte[] requestId, CachedRequest request);
    
    bool ContainsPendingRequest(byte[] requestId);

    bool ContainsCachedRequest(byte[] requestId);
    
    PendingRequest? GetPendingRequest(byte[] requestId);

    CachedRequest? GetCachedRequest(byte[] requestId);
    
    List<PendingRequest> GetPendingRequests();

    void MarkRequestAsFulfilled(byte[] requestId);

    void MarkCachedRequestAsFulfilled(byte[] requestId);
}