using Lantern.Discv5.Rlp;

namespace Lantern.Discv5.WireProtocol.Message.Requests;

public class FindNodeMessage : Message
{
    public FindNodeMessage(int[] distances) : base(MessageType.FindNode)
    {
        Distances = distances;
    }

    public int[] Distances { get; }

    public override byte[] EncodeMessage()
    {
        var messageId = new[] { (byte)MessageType };
        var encodedRequestId = RlpEncoder.EncodeBytes(RequestId);
        var encodedDistances = EncodeDistances();
        var encodedMessage =
            RlpEncoder.EncodeCollectionOfBytes(ByteArrayUtils.Concatenate(encodedRequestId, encodedDistances));
        return ByteArrayUtils.Concatenate(messageId, encodedMessage);
    }
    
    private byte[] EncodeDistances()
    {
        using var stream = new MemoryStream();
        foreach (var distance in Distances) 
            stream.Write(RlpEncoder.EncodeInteger(distance));
        return RlpEncoder.EncodeCollectionOfBytes(stream.ToArray());
    }
}