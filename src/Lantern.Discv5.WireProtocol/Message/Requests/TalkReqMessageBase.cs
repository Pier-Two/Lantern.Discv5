using Lantern.Discv5.Rlp;

namespace Lantern.Discv5.WireProtocol.Message.Requests;

public class TalkReqMessageBase : MessageBase
{
    public TalkReqMessageBase(byte[] protocol, byte[] request) : base(Discv5.WireProtocol.Message.MessageType.TalkReq)
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

    public static TalkReqMessageBase DecodeMessage(byte[] message)
    {
        var decodedMessage = RlpDecoder.Decode(message[1..]);
        return new TalkReqMessageBase(decodedMessage[1], decodedMessage[2])
        {
            RequestId = decodedMessage[0]
        };
    }
}