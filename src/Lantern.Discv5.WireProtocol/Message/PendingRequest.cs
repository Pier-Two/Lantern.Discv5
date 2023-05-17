namespace Lantern.Discv5.WireProtocol.Message;

public class PendingRequest
{
    public byte[] NodeId { get; }
    
    public Message Message { get; }
    
    public DateTime CreatedAt { get; } = DateTime.UtcNow;
    
    public PendingRequest(byte[] nodeId, Message message)
    {
        NodeId = nodeId;
        Message = message;
    }
}