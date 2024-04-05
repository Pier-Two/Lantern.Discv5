namespace Lantern.Discv5.WireProtocol.Message.Responses;

public class PongResponseEventArgs(IEnumerable<byte> requestId, PongMessage pongMessage) : EventArgs
{
    public IEnumerable<byte> RequestId { get; } = requestId;
    
    public PongMessage PongMessage { get; } = pongMessage;
}