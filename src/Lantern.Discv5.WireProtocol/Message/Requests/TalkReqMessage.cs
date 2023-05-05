using Lantern.Discv5.Rlp;

namespace Lantern.Discv5.WireProtocol.Message.Requests;

public class TalkReqMessage : Message
{
    public TalkReqMessage(byte[] protocol, byte[] request) : base(MessageType.TalkReq)
    {
        Protocol = protocol;
        Request = request;
    }

    public byte[] Protocol { get; }

    public byte[] Request { get; }

    public override byte[] EncodeMessage()
    {
        var messageId = new[] { (byte)MessageType };
        var encodedRequestId = RlpEncoder.EncodeBytes(RequestId);
        var encodedProtocol = RlpEncoder.EncodeBytes(Protocol);
        var encodedRequest = RlpEncoder.EncodeBytes(Request);
        var encodedMessage =
            RlpEncoder.EncodeCollectionOfBytes(ByteArrayUtils.Concatenate(encodedRequestId, encodedProtocol,
                encodedRequest));
        return ByteArrayUtils.Concatenate(messageId, encodedMessage);
    }
}