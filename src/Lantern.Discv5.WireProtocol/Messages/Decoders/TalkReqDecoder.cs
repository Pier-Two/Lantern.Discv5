using Lantern.Discv5.Rlp;
using Lantern.Discv5.WireProtocol.Messages.Requests;

namespace Lantern.Discv5.WireProtocol.Messages.Decoders;

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