using System.Net;
using Lantern.Discv5.Rlp;

namespace Lantern.Discv5.WireProtocol.Message.Responses;

public class PongMessageBase : MessageBase
{
    public PongMessageBase(int enrSeq, IPAddress recipientIp, int recipientPort) : base(Discv5.WireProtocol.Message.MessageType.Pong)
    {
        EnrSeq = enrSeq;
        RecipientIp = recipientIp;
        RecipientPort = recipientPort;
    }

    private PongMessageBase(byte[] requestId, int enrSeq, IPAddress recipientIp, int recipientPort) : base(
        Discv5.WireProtocol.Message.MessageType.Pong)
    {
        RequestId = requestId;
        EnrSeq = enrSeq;
        RecipientIp = recipientIp;
        RecipientPort = recipientPort;
    }

    public int EnrSeq { get; }

    public IPAddress RecipientIp { get; }

    public int RecipientPort { get; }

    public override byte[] EncodeMessage()
    {
        var messageId = new[] { MessageType };
        var encodedRequestId = RlpEncoder.EncodeBytes(RequestId);
        var encodedEnrSeq = RlpEncoder.EncodeInteger(EnrSeq);
        var encodedRecipientIp = RlpEncoder.EncodeBytes(RecipientIp.GetAddressBytes());
        var encodedRecipientPort = RlpEncoder.EncodeInteger(RecipientPort);
        var encodedMessage = RlpEncoder.EncodeCollectionOfBytes(ByteArrayUtils.Concatenate(encodedRequestId,
            encodedEnrSeq, encodedRecipientIp, encodedRecipientPort));
        return ByteArrayUtils.Concatenate(messageId, encodedMessage);
    }

    public static PongMessageBase DecodeMessage(byte[] message)
    {
        var decodedMessage = RlpDecoder.Decode(message[1..]);
        return new PongMessageBase(decodedMessage[0], RlpExtensions.ByteArrayToInt32(decodedMessage[1]),
            new IPAddress(decodedMessage[2]), RlpExtensions.ByteArrayToInt32(decodedMessage[3]));
    }
}