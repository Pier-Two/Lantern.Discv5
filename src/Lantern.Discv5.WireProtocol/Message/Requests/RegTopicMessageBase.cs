using Lantern.Discv5.Enr;
using Lantern.Discv5.Rlp;

namespace Lantern.Discv5.WireProtocol.Message.Requests;

public class RegTopicMessageBase : MessageBase
{
    public RegTopicMessageBase(byte[] topic, EnrRecord enr, byte[] ticket) : base(Discv5.WireProtocol.Message.MessageType.RegTopic)
    {
        Topic = topic;
        Enr = enr;
        Ticket = ticket;
    }

    public byte[] Topic { get; }

    public EnrRecord Enr { get; }

    public byte[] Ticket { get; }

    public override byte[] EncodeMessage()
    {
        var messageId = new[] { MessageType };
        var encodedRequestId = RlpEncoder.EncodeBytes(RequestId);
        var encodedTopic = RlpEncoder.EncodeBytes(Topic);
        var encodedEnr = Enr.EncodeEnrRecord();
        var encodedTicket = RlpEncoder.EncodeBytes(Ticket);
        var encodedMessage =
            RlpEncoder.EncodeCollectionOfBytes(ByteArrayUtils.Concatenate(encodedRequestId, encodedTopic,
                encodedEnr, encodedTicket));
        return ByteArrayUtils.Concatenate(messageId, encodedMessage);
    }

    public static RegTopicMessageBase DecodeMessage(byte[] message)
    {
        var decodedMessage = RlpDecoder.Decode(message[1..]);
        var decodedTopic = decodedMessage[1];
        var decodedEnr = decodedMessage.GetRange(2, decodedMessage.Count - 2).SkipLast(1).ToList();
        var enr = new EnrRecordFactory().CreateFromDecoded(decodedEnr.ToList());
        var decodedTicket = decodedMessage.Last();
        return new RegTopicMessageBase(decodedTopic, enr, decodedTicket)
        {
            RequestId = decodedMessage[0]
        };
    }
}