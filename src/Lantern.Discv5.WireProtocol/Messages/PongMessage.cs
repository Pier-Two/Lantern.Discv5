using System.Net;
using Lantern.Discv5.Rlp;

namespace Lantern.Discv5.WireProtocol.Messages;

public class PongMessage : Message
{
    public PongMessage(int enrSeq, IPAddress recipientIp, int recipientPort) : base(Messages.MessageType.Pong)
    {
        EnrSeq = enrSeq;
        RecipientIp = recipientIp;
        RecipientPort = recipientPort;
    }

    private PongMessage(byte[] requestId, int enrSeq, IPAddress recipientIp, int recipientPort) : base(
        Messages.MessageType.Pong)
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
        var encodedMessage = RlpEncoder.EncodeCollectionOfBytes(Helpers.JoinMultipleByteArrays(encodedRequestId,
            encodedEnrSeq, encodedRecipientIp, encodedRecipientPort));
        return Helpers.JoinMultipleByteArrays(messageId, encodedMessage);
    }

    public static PongMessage DecodeMessage(byte[] message)
    {
        var decodedMessage = RlpDecoder.Decode(message[1..]);
        return new PongMessage(decodedMessage[0], RlpExtensions.ByteArrayToInt32(decodedMessage[1]),
            new IPAddress(decodedMessage[2]), RlpExtensions.ByteArrayToInt32(decodedMessage[3]));
    }
}