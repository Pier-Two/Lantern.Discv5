using System.Security.Cryptography;
using Lantern.Discv5.WireProtocol.Utility;

namespace Lantern.Discv5.WireProtocol.Messages;

public abstract class Message
{
    protected Message(MessageType messageType, byte[]? requestId = null)
    {
        MessageType = messageType;
        RequestId = requestId ?? MessageUtils.GenerateRequestId();
    }
    
    public MessageType MessageType { get; }

    public byte[] RequestId { get; set; }

    public abstract byte[] EncodeMessage();
}