using Lantern.Discv5.Rlp;

namespace Lantern.Discv5.WireProtocol.Messages.Requests;

public class PingMessage : Message
{
    public PingMessage(int enrSeq) : base(MessageType.Ping)
    {
        EnrSeq = enrSeq;
    }

    public int EnrSeq { get; }

    public override byte[] EncodeMessage()
    {
        var messageId = new[] { (byte)MessageType };
        var encodedRequestId = RlpEncoder.EncodeBytes(RequestId);
        var encodedEnrSeq = RlpEncoder.EncodeInteger(EnrSeq);
        var encodedMessage =
            RlpEncoder.EncodeCollectionOfBytes(ByteArrayUtils.Concatenate(encodedRequestId, encodedEnrSeq));
        return ByteArrayUtils.Concatenate(messageId, encodedMessage);
    }
}