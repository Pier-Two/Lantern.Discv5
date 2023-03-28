using Lantern.Discv5.Rlp;

namespace Lantern.Discv5.WireProtocol.Message.Responses;

public class TicketMessageBase : MessageBase
{
    public TicketMessageBase(byte[] ticket, int waitTime) : base(Discv5.WireProtocol.Message.MessageType.Ticket)
    {
        Ticket = ticket;
        WaitTime = waitTime;
    }

    private TicketMessageBase(byte[] requestId, byte[] ticket, int waitTime) : base(Discv5.WireProtocol.Message.MessageType.Ticket)
    {
        Ticket = ticket;
        WaitTime = waitTime;
        RequestId = requestId;
    }

    public byte[] Ticket { get; }

    public int WaitTime { get; }

    public override byte[] EncodeMessage()
    {
        var messageId = new[] { MessageType };
        var encodedRequestId = RlpEncoder.EncodeBytes(RequestId);
        var encodedTicket = RlpEncoder.EncodeBytes(Ticket);
        var encodedWaitTime = RlpEncoder.EncodeInteger(WaitTime);
        var encodedMessage =
            RlpEncoder.EncodeCollectionOfBytes(ByteArrayUtils.Concatenate(encodedRequestId, encodedTicket,
                encodedWaitTime));
        return ByteArrayUtils.Concatenate(messageId, encodedMessage);
    }

    public static TicketMessageBase DecodeMessage(byte[] message)
    {
        var decodedMessage = RlpDecoder.Decode(message[1..]);
        return new TicketMessageBase(decodedMessage[0], decodedMessage[1],
            RlpExtensions.ByteArrayToInt32(decodedMessage[2]));
    }
}