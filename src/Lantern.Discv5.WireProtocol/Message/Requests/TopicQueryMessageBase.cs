using Lantern.Discv5.Rlp;

namespace Lantern.Discv5.WireProtocol.Message.Requests;

public class TopicQueryMessageBase : MessageBase
{
    public TopicQueryMessageBase(byte[] topic) : base(Discv5.WireProtocol.Message.MessageType.TopicQuery)
    {
        Topic = topic;
    }

    public byte[] Topic { get; }

    public override byte[] EncodeMessage()
    {
        var messageId = new[] { MessageType };
        var encodedRequestId = RlpEncoder.EncodeBytes(RequestId);
        var encodedTopic = RlpEncoder.EncodeBytes(Topic);
        var encodedMessage =
            RlpEncoder.EncodeCollectionOfBytes(ByteArrayUtils.Concatenate(encodedRequestId, encodedTopic));
        return ByteArrayUtils.Concatenate(messageId, encodedMessage);
    }

    public static TopicQueryMessageBase DecodeMessage(byte[] message)
    {
        var decodedMessage = RlpDecoder.Decode(message[1..]);
        return new TopicQueryMessageBase(decodedMessage[1])
        {
            RequestId = decodedMessage[0]
        };
    }
}