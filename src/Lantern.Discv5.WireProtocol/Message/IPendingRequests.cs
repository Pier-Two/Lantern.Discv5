namespace Lantern.Discv5.WireProtocol.Message;

public interface IPendingRequests
{
    bool AddPendingRequest(byte[] requestId, PendingRequest? request);
    
    bool ContainsPendingRequest(byte[] requestId);
    
    PendingRequest? GetPendingRequest(byte[] requestId);
    
    List<PendingRequest> GetPendingRequests();

    bool RemovePendingRequest(byte[] requestId);
}