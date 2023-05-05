using Lantern.Discv5.Rlp;
using Lantern.Discv5.WireProtocol.Message.Requests;

namespace Lantern.Discv5.WireProtocol.Message.Decoders;

public class TopicQueryDecoder : IMessageDecoder<TopicQueryMessage>
{
    public TopicQueryMessage DecodeMessage(byte[] message)
    {
        var decodedMessage = RlpDecoder.Decode(message[1..]);
        return new TopicQueryMessage(decodedMessage[1])
        {
            RequestId = decodedMessage[0]
        };
    }
}