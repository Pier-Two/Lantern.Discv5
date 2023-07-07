namespace Lantern.Discv5.WireProtocol.Message;

public interface IRequestManager
{
    void StartRequestManager();
    
    Task StopRequestManagerAsync();
    
    bool AddPendingRequest(byte[] requestId, PendingRequest request);

    bool AddCachedRequest(byte[] requestId, CachedRequest request);

    void AddCachedHandshakeInteraction(byte[] packetNonce, byte[] destNodeId);
    
    byte[]? GetCachedHandshakeInteraction(byte[] packetNonce);
    
    bool ContainsPendingRequest(byte[] requestId);

    bool ContainsCachedRequest(byte[] requestId);
    
    PendingRequest? GetPendingRequest(byte[] requestId);

    CachedRequest? GetCachedRequest(byte[] requestId);

    void MarkRequestAsFulfilled(byte[] requestId);

    void MarkCachedRequestAsFulfilled(byte[] requestId);
}