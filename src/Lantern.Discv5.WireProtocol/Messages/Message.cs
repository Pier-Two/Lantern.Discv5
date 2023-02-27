namespace Lantern.Discv5.WireProtocol.Messages;

public abstract class Message
{
    protected byte MessageType { get; }

    public byte[] RequestId { get; protected init; }

    public Message(byte messageType)
    {
        MessageType = messageType;
        RequestId = GenerateRequestId();
    }
    
    public static byte[] GenerateRequestId()
    {
        var requestId = new byte[8];
        Random.Shared.NextBytes(requestId);
        return requestId;
    }
    
    public abstract byte[] EncodeMessage();
}