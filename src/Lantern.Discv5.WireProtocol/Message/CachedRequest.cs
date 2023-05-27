namespace Lantern.Discv5.WireProtocol.Message;

public class CachedRequest
{
    public byte[] NodeId { get; }
    
    public Message Message { get; }
    
    public CachedRequest(byte[] nodeId, Message message)
    {
        NodeId = nodeId;
        Message = message;
    }
}