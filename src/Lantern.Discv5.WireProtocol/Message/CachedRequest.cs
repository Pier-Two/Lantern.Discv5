using System.Diagnostics;

namespace Lantern.Discv5.WireProtocol.Message;

public class CachedRequest
{
    public byte[] NodeId { get; }
    
    public Message Message { get; }
    
    public bool IsFulfilled { get; set; }
    
    public Stopwatch ElapsedTime { get; } = Stopwatch.StartNew();
    
    public CachedRequest(byte[] nodeId, Message message)
    {
        NodeId = nodeId;
        Message = message;
        IsFulfilled = false;
    }
}