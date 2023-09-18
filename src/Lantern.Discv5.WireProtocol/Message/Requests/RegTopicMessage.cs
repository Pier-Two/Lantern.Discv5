using Lantern.Discv5.Enr;
using Lantern.Discv5.Rlp;

namespace Lantern.Discv5.WireProtocol.Message.Requests;

public class RegTopicMessage : Message
{
    public RegTopicMessage(byte[] topic, Enr.Enr enr, byte[] ticket) : base(MessageType.RegTopic)
    {
        Topic = topic;
        Enr = enr;
        Ticket = ticket;
    }

    public byte[] Topic { get; }

    public Enr.Enr Enr { get; }

    public byte[] Ticket { get; }

    public override byte[] EncodeMessage()
    {
        var messageId = new[] { (byte)MessageType };
        var encodedRequestId = RlpEncoder.EncodeBytes(RequestId);
        var encodedTopic = RlpEncoder.EncodeBytes(Topic);
        var encodedEnr = Enr.EncodeRecord();
        var encodedTicket = RlpEncoder.EncodeBytes(Ticket);
        var encodedMessage =
            RlpEncoder.EncodeCollectionOfBytes(ByteArrayUtils.Concatenate(encodedRequestId, encodedTopic,
                encodedEnr, encodedTicket));
        return ByteArrayUtils.Concatenate(messageId, encodedMessage);
    }
}