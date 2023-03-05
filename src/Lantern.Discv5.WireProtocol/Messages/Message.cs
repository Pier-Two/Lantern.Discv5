namespace Lantern.Discv5.WireProtocol.Messages;

public abstract class Message
{
    protected Message(byte messageType)
    {
        MessageType = messageType;
        RequestId = GenerateRequestId();
    }

    protected byte MessageType { get; }

    public byte[] RequestId { get; protected init; }

    private static byte[] GenerateRequestId()
    {
        var requestId = new byte[8];
        Random.Shared.NextBytes(requestId);
        return requestId;
    }

    public abstract byte[] EncodeMessage();
}