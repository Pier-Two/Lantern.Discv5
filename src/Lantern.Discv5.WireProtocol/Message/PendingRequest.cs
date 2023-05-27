using System.Diagnostics;

namespace Lantern.Discv5.WireProtocol.Message;

public class PendingRequest
{
    public byte[] NodeId { get; }
    
    public Message Message { get; }

    public Stopwatch ElapsedTime { get; } = Stopwatch.StartNew();
    
    public bool IsFulfilled { get; set; }
    
    public int ResponsesReceived { get; set; }

    public PendingRequest(byte[] nodeId, Message message)
    {
        NodeId = nodeId;
        Message = message;
    }
}