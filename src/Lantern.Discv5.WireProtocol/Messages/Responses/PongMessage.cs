using System.Net;
using Lantern.Discv5.Rlp;

namespace Lantern.Discv5.WireProtocol.Messages.Responses;

public class PongMessage : Message
{
    public PongMessage(ulong enrSeq, IPAddress recipientIp, int recipientPort) : base(MessageType.Pong)
    {
        EnrSeq = enrSeq;
        RecipientIp = recipientIp;
        RecipientPort = recipientPort;
    }

    public PongMessage(byte[] requestId, ulong enrSeq, IPAddress recipientIp, int recipientPort) : base(
        MessageType.Pong, requestId)
    {
        EnrSeq = enrSeq;
        RecipientIp = recipientIp;
        RecipientPort = recipientPort;
    }

    public ulong EnrSeq { get; }

    public IPAddress RecipientIp { get; }

    public int RecipientPort { get; }

    public override byte[] EncodeMessage()
    {
        var messageId = new[] { (byte)MessageType };
        var encodedRequestId = RlpEncoder.EncodeBytes(RequestId);
        var encodedEnrSeq = RlpEncoder.EncodeUlong(EnrSeq);
        var encodedRecipientIp = RlpEncoder.EncodeBytes(RecipientIp.GetAddressBytes());
        var encodedRecipientPort = RlpEncoder.EncodeInteger(RecipientPort);
        var encodedMessage = RlpEncoder.EncodeCollectionOfBytes(ByteArrayUtils.Concatenate(encodedRequestId,
            encodedEnrSeq, encodedRecipientIp, encodedRecipientPort));
        return ByteArrayUtils.Concatenate(messageId, encodedMessage);
    }
}
