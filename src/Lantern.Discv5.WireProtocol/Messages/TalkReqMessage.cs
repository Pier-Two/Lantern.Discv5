using Lantern.Discv5.Rlp;

namespace Lantern.Discv5.WireProtocol.Messages;

public class TalkReqMessage : Message
{
    public TalkReqMessage(byte[] protocol, byte[] request) : base(Messages.MessageType.TalkReq)
    {
        Protocol = protocol;
        Request = request;
    }

    public byte[] Protocol { get; }

    public byte[] Request { get; }

    public override byte[] EncodeMessage()
    {
        var messageId = new[] { MessageType };
        var encodedRequestId = RlpEncoder.EncodeBytes(RequestId);
        var encodedProtocol = RlpEncoder.EncodeBytes(Protocol);
        var encodedRequest = RlpEncoder.EncodeBytes(Request);
        var encodedMessage =
            RlpEncoder.EncodeCollectionOfBytes(ByteArrayUtils.Concatenate(encodedRequestId, encodedProtocol,
                encodedRequest));
        return ByteArrayUtils.Concatenate(messageId, encodedMessage);
    }

    public static TalkReqMessage DecodeMessage(byte[] message)
    {
        var decodedMessage = RlpDecoder.Decode(message[1..]);
        return new TalkReqMessage(decodedMessage[1], decodedMessage[2])
        {
            RequestId = decodedMessage[0]
        };
    }
}