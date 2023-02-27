using Lantern.Discv5.Enr;
using Lantern.Discv5.Rlp;

namespace Lantern.Discv5.WireProtocol.Messages;

public class Nodes : Message
{
    public int Total { get; }
    
    public string[] Enrs { get; }
    
    public Nodes(int total, string[] enrs) : base(MessageTypes.Nodes)
    {
        Total = total;
        Enrs = enrs;
    }
    
    public Nodes(byte[] requestId, int total, string[] enrs) : base(MessageTypes.Nodes)
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
        var encodedEnrs =  RlpEncoder.EncodeCollectionOfBytes(EnrRecordExtensions.EnrStringToBytes(Enrs));
        var encodedItems = RlpEncoder.EncodeCollectionOfBytes(Helpers.JoinMultipleByteArrays(encodedRequestId, encodedTotal, encodedEnrs));
        return Helpers.JoinMultipleByteArrays(messageId, encodedItems);
    }
    
    

    /*
    public static Nodes DecodeMessage(byte[] message)
    {
        var decodedMessage = RlpDecoder.Decode(message[1..]);
        var total = RlpExtensions.ByteArrayToInt32(decodedMessage[1]);
        var enrs = new EnrRecord[decodedMessage.Count - 2];

        for (var i = 2; i < decodedMessage.Count; i++)
        {
            enrs[i - 2] = EnrRecordExtensions.FromBytes(decodedMessage[i]);
        }
        
        return new Nodes(total, enrs);
    }*/
}