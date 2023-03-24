using Lantern.Discv5.Rlp;

namespace Lantern.Discv5.WireProtocol.Messages;

public class RegConfirmationMessage : Message
{
    public RegConfirmationMessage(byte[] topic) : base(Messages.MessageType.RegConfirmation)
    {
        Topic = topic;
    }

    private RegConfirmationMessage(byte[] requestId, byte[] topic) : base(Messages.MessageType.RegConfirmation)
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

    public static RegConfirmationMessage DecodeMessage(byte[] message)
    {
        var decodedMessage = RlpDecoder.Decode(message[1..]);
        return new RegConfirmationMessage(decodedMessage[0], decodedMessage[1]);
    }
}