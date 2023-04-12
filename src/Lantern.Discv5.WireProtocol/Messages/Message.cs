using System.Security.Cryptography;

namespace Lantern.Discv5.WireProtocol.Messages;

public abstract class Message
{
    protected Message(MessageType messageType, byte[]? requestId = null)
    {
        MessageType = messageType;
        RequestId = requestId ?? GenerateRequestId();
    }
    
    public MessageType MessageType { get; }

    public byte[] RequestId { get; set; }

    private static byte[] GenerateRequestId()
    {
        var requestId = new byte[8];
        var random = RandomNumberGenerator.Create(); 
        random.GetBytes(requestId);
        return requestId;
    }

    public abstract byte[] EncodeMessage();
}