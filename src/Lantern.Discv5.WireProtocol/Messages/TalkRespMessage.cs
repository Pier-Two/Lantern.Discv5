using Lantern.Discv5.Rlp;

namespace Lantern.Discv5.WireProtocol.Messages;

public class TalkRespMessage : Message
{
    public TalkRespMessage(byte[] response) : base(Messages.MessageType.TalkResp)
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

    public static TalkRespMessage DecodeMessage(byte[] message)
    {
        var decodedMessage = RlpDecoder.Decode(message[1..]);
        return new TalkRespMessage(decodedMessage[1])
        {
            RequestId = decodedMessage[0]
        };
    }
}