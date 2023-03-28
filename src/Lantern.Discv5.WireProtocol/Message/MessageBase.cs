namespace Lantern.Discv5.WireProtocol.Message;

public abstract class MessageBase
{
    protected MessageBase(byte messageType)
    {
        MessageType = messageType;
        RequestId = GenerateRequestId();
    }

    protected byte MessageType { get; }

    public byte[] RequestId { get; set; }

    private static byte[] GenerateRequestId()
    {
        var requestId = new byte[8];
        Random.Shared.NextBytes(requestId);
        return requestId;
    }

    public abstract byte[] EncodeMessage();
}