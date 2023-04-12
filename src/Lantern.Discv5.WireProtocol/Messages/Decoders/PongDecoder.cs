using System.Net;
using Lantern.Discv5.Rlp;
using Lantern.Discv5.WireProtocol.Messages.Responses;

namespace Lantern.Discv5.WireProtocol.Messages.Decoders;

public class PongDecoder : IMessageDecoder<PongMessage>
{
    public PongMessage DecodeMessage(byte[] message)
    {
        var decodedMessage = RlpDecoder.Decode(message[1..]);
        return new PongMessage(decodedMessage[0], RlpExtensions.ByteArrayToInt32(decodedMessage[1]),
            new IPAddress(decodedMessage[2]), RlpExtensions.ByteArrayToInt32(decodedMessage[3]));
    }
}