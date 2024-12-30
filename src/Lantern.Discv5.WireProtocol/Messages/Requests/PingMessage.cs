using Lantern.Discv5.Rlp;

namespace Lantern.Discv5.WireProtocol.Messages.Requests;

public class PingMessage(ulong enrSeq) : Message(MessageType.Ping)
{
    public ulong EnrSeq { get; } = enrSeq;

    public override byte[] EncodeMessage()
    {
        var messageId = new[] { (byte)MessageType };
        var encodedRequestId = RlpEncoder.EncodeBytes(RequestId);
        var encodedEnrSeq = RlpEncoder.EncodeUlong(EnrSeq);
        var encodedMessage =
            RlpEncoder.EncodeCollectionOfBytes(ByteArrayUtils.Concatenate(encodedRequestId, encodedEnrSeq));
        return ByteArrayUtils.Concatenate(messageId, encodedMessage);
    }
}
