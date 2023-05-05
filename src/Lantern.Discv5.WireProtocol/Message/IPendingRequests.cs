namespace Lantern.Discv5.WireProtocol.Message;

public interface IPendingRequests
{
    public bool AddPendingRequest(byte[] requestId, PendingRequest? request);
    
    public bool ContainsPendingRequest(byte[] requestId);
    
    public PendingRequest? GetPendingRequest(byte[] requestId);
    
    public bool RemovePendingRequest(byte[] requestId);
}