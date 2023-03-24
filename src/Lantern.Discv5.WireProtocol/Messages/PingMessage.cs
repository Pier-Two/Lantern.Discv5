using Lantern.Discv5.Rlp;

namespace Lantern.Discv5.WireProtocol.Messages;

public class PingMessage : Message
{
    public PingMessage(int enrSeq) : base(Messages.MessageType.Ping)
    {
        EnrSeq = enrSeq;
    }

    public int EnrSeq { get; }

    public override byte[] EncodeMessage()
    {
        var messageId = new[] { MessageType };
        var encodedRequestId = RlpEncoder.EncodeBytes(RequestId);
        var encodedEnrSeq = RlpEncoder.EncodeInteger(EnrSeq);
        var encodedMessage =
            RlpEncoder.EncodeCollectionOfBytes(ByteArrayUtils.Concatenate(encodedRequestId, encodedEnrSeq));
        return ByteArrayUtils.Concatenate(messageId, encodedMessage);
    }

    public static PingMessage DecodeMessage(byte[] message)
    {
        var decodedMessage = RlpDecoder.Decode(message[1..]);
        return new PingMessage(RlpExtensions.ByteArrayToInt32(decodedMessage[1]))
        {
            RequestId = decodedMessage[0]
        };
    }
}