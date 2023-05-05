namespace Lantern.Discv5.WireProtocol.Message;

public abstract class Message
{
    protected Message(MessageType messageType, byte[]? requestId = null)
    {
        MessageType = messageType;
        RequestId = requestId ?? MessageUtility.GenerateRequestId(MessageConstants.RequestIdLength);
    }
    
    public MessageType MessageType { get; }

    public byte[] RequestId { get; set; }

    public abstract byte[] EncodeMessage();
}