using Lantern.Discv5.Rlp;
using Lantern.Discv5.WireProtocol.Message.Requests;

namespace Lantern.Discv5.WireProtocol.Message.Decoders;

public class FindNodeDecoder : IMessageDecoder<FindNodeMessage>
{
    public FindNodeMessage DecodeMessage(byte[] message)
    {
        var decodedMessage = RlpDecoder.Decode(message[1..]);
        var distances = new List<int>(decodedMessage.Count - 1);

        for (var i = 1; i < decodedMessage.Count; i++)
            distances.Add(RlpExtensions.ByteArrayToInt32(decodedMessage[i]));

        return new FindNodeMessage(distances.ToArray())
        {
            RequestId = decodedMessage[0]
        };
    }
}