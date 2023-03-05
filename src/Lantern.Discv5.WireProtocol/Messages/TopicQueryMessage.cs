using Lantern.Discv5.Rlp;

namespace Lantern.Discv5.WireProtocol.Messages;

public class TopicQueryMessage : Message
{
    public TopicQueryMessage(byte[] topic) : base(Messages.MessageType.TopicQuery)
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
            RlpEncoder.EncodeCollectionOfBytes(Helpers.JoinMultipleByteArrays(encodedRequestId, encodedTopic));
        return Helpers.JoinMultipleByteArrays(messageId, encodedMessage);
    }

    public static TopicQueryMessage DecodeMessage(byte[] message)
    {
        var decodedMessage = RlpDecoder.Decode(message[1..]);
        return new TopicQueryMessage(decodedMessage[1])
        {
            RequestId = decodedMessage[0]
        };
    }
}