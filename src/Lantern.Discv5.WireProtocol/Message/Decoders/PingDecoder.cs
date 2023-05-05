using Lantern.Discv5.Rlp;
using Lantern.Discv5.WireProtocol.Message.Requests;

namespace Lantern.Discv5.WireProtocol.Message.Decoders;

public class PingDecoder : IMessageDecoder<PingMessage>
{
    public PingMessage DecodeMessage(byte[] message)
    {
        var decodedMessage = RlpDecoder.Decode(message[1..]);
        return new PingMessage(RlpExtensions.ByteArrayToInt32(decodedMessage[1]))
        {
            RequestId = decodedMessage[0]
        };
    }
}