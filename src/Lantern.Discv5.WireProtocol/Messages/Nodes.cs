using Lantern.Discv5.Enr;
using Lantern.Discv5.Rlp;

namespace Lantern.Discv5.WireProtocol.Messages;

public class Nodes : Message
{
    public int Total { get; }
    
    public EnrRecord[] Enrs { get; }
    
    public Nodes(int total, EnrRecord[] enrs) : base(MessageTypes.Nodes)
    {
        Total = total;
        Enrs = enrs;
    }
    
    public Nodes(byte[] requestId, int total, EnrRecord[] enrs) : base(MessageTypes.Nodes)
    {
        RequestId = requestId;
        Total = total;
        Enrs = enrs;
    }
    
    public override byte[] EncodeMessage()
    { 
        var messageId = new [] { MessageType };
        var encodedRequestId = RlpEncoder.EncodeBytes(RequestId);
        var encodedTotal = RlpEncoder.EncodeInteger(Total);

        using var stream = new MemoryStream();
        
        foreach (var enr in Enrs)
        {
            stream.Write(enr.EncodeEnrRecord());
        }
        
        var encodedEnrs =  RlpEncoder.EncodeCollectionOfBytes(stream.ToArray());
        var encodedItems = RlpEncoder.EncodeCollectionOfBytes(Helpers.JoinMultipleByteArrays(encodedRequestId, encodedTotal, encodedEnrs));
        return Helpers.JoinMultipleByteArrays(messageId, encodedItems);
    }

    public static Nodes DecodeMessage(byte[] message)
    {
        var rawMessage = message[1..];


        return null;
    }
    



}


