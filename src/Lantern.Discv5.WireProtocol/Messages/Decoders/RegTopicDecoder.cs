using Lantern.Discv5.Enr.EnrFactory;
using Lantern.Discv5.Rlp;
using Lantern.Discv5.WireProtocol.Messages.Requests;

namespace Lantern.Discv5.WireProtocol.Messages.Decoders;

public class RegTopicDecoder : IMessageDecoder<RegTopicMessage>
{
    
    public RegTopicMessage DecodeMessage(byte[] message)
    {
        var decodedMessage = RlpDecoder.Decode(message[1..]);
        var decodedTopic = decodedMessage[1];
        var decodedEnr = decodedMessage.GetRange(2, decodedMessage.Count - 2).SkipLast(1).ToList();
        var enr = new EnrRecordFactory().CreateFromDecoded(decodedEnr.ToList());
        var decodedTicket = decodedMessage.Last();
        return new RegTopicMessage(decodedTopic, enr, decodedTicket)
        {
            RequestId = decodedMessage[0]
        };
    }
}