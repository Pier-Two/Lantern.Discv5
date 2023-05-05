namespace Lantern.Discv5.WireProtocol.Message;

public class PendingRequest
{
    public byte[] NodeId { get; set; }
    
    public Message Message { get; set; }
    
    public PendingRequest(byte[] nodeId, Message message)
    {
        NodeId = nodeId;
        Message = message;
    }
}