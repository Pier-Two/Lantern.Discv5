using Lantern.Discv5.Rlp;

namespace Lantern.Discv5.WireProtocol.Message.Responses;

public class RegConfirmationMessageBase : MessageBase
{
    public RegConfirmationMessageBase(byte[] topic) : base(Discv5.WireProtocol.Message.MessageType.RegConfirmation)
    {
        Topic = topic;
    }

    private RegConfirmationMessageBase(byte[] requestId, byte[] topic) : base(Discv5.WireProtocol.Message.MessageType.RegConfirmation)
    {
        RequestId = requestId;
        Topic = topic;
    }

    public byte[] Topic { get; }

    public override byte[] EncodeMessage()
    {
        var messageId = new[] { MessageType };
        var encodedRequestId = RlpEncoder.EncodeBytes(RequestId);
        var encodedTopic = RlpEncoder.EncodeBytes(Topic);
        var encodedMessage =
            RlpEncoder.EncodeCollectionOfBytes(ByteArrayUtils.Concatenate(encodedRequestId, encodedTopic));
        return ByteArrayUtils.Concatenate(messageId, encodedMessage);
    }

    public static RegConfirmationMessageBase DecodeMessage(byte[] message)
    {
        var decodedMessage = RlpDecoder.Decode(message[1..]);
        return new RegConfirmationMessageBase(decodedMessage[0], decodedMessage[1]);
    }
}