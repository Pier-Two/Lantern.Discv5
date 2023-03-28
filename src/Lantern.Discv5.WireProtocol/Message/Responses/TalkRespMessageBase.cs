using Lantern.Discv5.Rlp;

namespace Lantern.Discv5.WireProtocol.Message.Responses;

public class TalkRespMessageBase : MessageBase
{
    public TalkRespMessageBase(byte[] response) : base(Discv5.WireProtocol.Message.MessageType.TalkResp)
    {
        Response = response;
    }

    public byte[] Response { get; }

    public override byte[] EncodeMessage()
    {
        var messageId = new[] { MessageType };
        var encodedRequestId = RlpEncoder.EncodeBytes(RequestId);
        var encodedResponse = RlpEncoder.EncodeBytes(Response);
        var encodedMessage =
            RlpEncoder.EncodeCollectionOfBytes(ByteArrayUtils.Concatenate(encodedRequestId, encodedResponse));
        return ByteArrayUtils.Concatenate(messageId, encodedMessage);
    }

    public static TalkRespMessageBase DecodeMessage(byte[] message)
    {
        var decodedMessage = RlpDecoder.Decode(message[1..]);
        return new TalkRespMessageBase(decodedMessage[1])
        {
            RequestId = decodedMessage[0]
        };
    }
}