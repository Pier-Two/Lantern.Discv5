using Lantern.Discv5.Rlp;

namespace Lantern.Discv5.WireProtocol.Messages.Requests;

public class TopicQueryMessage : Message
{
    public TopicQueryMessage(byte[] topic) : base(MessageType.TopicQuery)
    {
        Topic = topic;
    }

    public byte[] Topic { get; }

    public override byte[] EncodeMessage()
    {
        var messageId = new[] { (byte)MessageType };
        var encodedRequestId = RlpEncoder.EncodeBytes(RequestId);
        var encodedTopic = RlpEncoder.EncodeBytes(Topic);
        var encodedMessage =
            RlpEncoder.EncodeCollectionOfBytes(ByteArrayUtils.Concatenate(encodedRequestId, encodedTopic));
        return ByteArrayUtils.Concatenate(messageId, encodedMessage);
    }
}