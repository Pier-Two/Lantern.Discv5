using Lantern.Discv5.Rlp;
using Lantern.Discv5.WireProtocol.Messages.Responses;

namespace Lantern.Discv5.WireProtocol.Messages.Decoders;

public class TicketDecoder : IMessageDecoder<TicketMessage>
{
    public TicketMessage DecodeMessage(byte[] message)
    {
        var decodedMessage = RlpDecoder.Decode(message[1..]);
        return new TicketMessage(decodedMessage[0], decodedMessage[1],
            RlpExtensions.ByteArrayToInt32(decodedMessage[2]));
    } 
}