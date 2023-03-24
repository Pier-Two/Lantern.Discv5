using Lantern.Discv5.Enr;
using Lantern.Discv5.Rlp;

namespace Lantern.Discv5.WireProtocol.Messages;

public class RegTopicMessage : Message
{
    public RegTopicMessage(byte[] topic, EnrRecord enr, byte[] ticket) : base(Messages.MessageType.RegTopic)
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

    public static RegTopicMessage DecodeMessage(byte[] message)
    {
        var decodedMessage = RlpDecoder.Decode(message[1..]);
        var decodedTopic = decodedMessage[1];
        var decodedEnr = decodedMessage.GetRange(2, decodedMessage.Count - 2).SkipLast(1).ToList();

        var enr = EnrRecordExtensions.CreateEnrRecordFromDecoded(decodedEnr.ToList());
        var decodedTicket = decodedMessage.Last();
        return new RegTopicMessage(decodedTopic, enr, decodedTicket)
        {
            RequestId = decodedMessage[0]
        };
    }
}