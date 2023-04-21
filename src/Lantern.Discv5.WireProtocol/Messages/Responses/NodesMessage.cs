using Lantern.Discv5.Enr;
using Lantern.Discv5.Enr.EnrFactory;
using Lantern.Discv5.Rlp;

namespace Lantern.Discv5.WireProtocol.Messages.Responses;

public class NodesMessage : Message
{
    public NodesMessage(int total, EnrRecord[] enrs) : base(MessageType.Nodes)
    {
        Total = total;
        Enrs = enrs;
    }

    public NodesMessage(byte[] requestId, int total, EnrRecord[] enrs) : base(MessageType.Nodes, requestId)
    {
        Total = total;
        Enrs = enrs;
    }

    public int Total { get; }

    public EnrRecord[] Enrs { get; }
    
    public override byte[] EncodeMessage()
    {
        var messageId = new[] { (byte)MessageType };
        var encodedRequestId = RlpEncoder.EncodeBytes(RequestId);
        var encodedTotal = RlpEncoder.EncodeInteger(Total);
        var encodedEnrs = EncodeEnrs();
        var encodedItems =
            RlpEncoder.EncodeCollectionOfBytes(ByteArrayUtils.Concatenate(encodedRequestId, encodedTotal,
                encodedEnrs));
        return ByteArrayUtils.Concatenate(messageId, encodedItems);
    }
    
    private byte[] EncodeEnrs()
    {
        using var stream = new MemoryStream();
        foreach (var enr in Enrs) stream.Write(enr.EncodeEnrRecord());
        return RlpEncoder.EncodeCollectionOfBytes(stream.ToArray());
    }
}