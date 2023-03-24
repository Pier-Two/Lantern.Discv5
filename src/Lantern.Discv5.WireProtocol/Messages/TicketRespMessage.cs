using Lantern.Discv5.Rlp;

namespace Lantern.Discv5.WireProtocol.Messages;

public class TicketRespMessage : Message
{
    public TicketRespMessage(byte[] ticket, int waitTime) : base(Messages.MessageType.Ticket)
    {
        Ticket = ticket;
        WaitTime = waitTime;
    }

    private TicketRespMessage(byte[] requestId, byte[] ticket, int waitTime) : base(Messages.MessageType.Ticket)
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

    public static TicketRespMessage DecodeMessage(byte[] message)
    {
        var decodedMessage = RlpDecoder.Decode(message[1..]);
        return new TicketRespMessage(decodedMessage[0], decodedMessage[1],
            RlpExtensions.ByteArrayToInt32(decodedMessage[2]));
    }
}