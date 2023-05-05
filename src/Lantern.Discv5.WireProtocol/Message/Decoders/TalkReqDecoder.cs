using Lantern.Discv5.Rlp;
using Lantern.Discv5.WireProtocol.Message.Requests;

namespace Lantern.Discv5.WireProtocol.Message.Decoders;

public class TalkReqDecoder : IMessageDecoder<TalkReqMessage>
{
    public TalkReqMessage DecodeMessage(byte[] message)
    {
        var decodedMessage = RlpDecoder.Decode(message[1..]);
        return new TalkReqMessage(decodedMessage[1], decodedMessage[2])
        {
            RequestId = decodedMessage[0]
        };
    }
}