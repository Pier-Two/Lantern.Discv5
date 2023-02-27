using Lantern.Discv5.Rlp;

namespace Lantern.Discv5.WireProtocol.Messages;

public class Ping : Message
{
    public int EnrSeq { get; }
    
    public Ping(int enrSeq) : base(MessageTypes.Ping)
    {
        EnrSeq = enrSeq;
    }

    public override byte[] EncodeMessage()
    {
        var messageId = new [] { MessageType };
        var encodedRequestId = RlpEncoder.EncodeBytes(RequestId);
        var encodedEnrSeq = RlpEncoder.EncodeInteger(EnrSeq);
        var encodedMessage = RlpEncoder.EncodeCollectionOfBytes(Helpers.JoinMultipleByteArrays(encodedRequestId, encodedEnrSeq));
        return Helpers.JoinMultipleByteArrays(messageId, encodedMessage);
    }

    public static Ping DecodeMessage(byte[] message)
    {
        var decodedMessage = RlpDecoder.Decode(message[1..]);
        return new Ping(RlpExtensions.ByteArrayToInt32(decodedMessage[1]))
        {
            RequestId = decodedMessage[0]
        };
    }
}