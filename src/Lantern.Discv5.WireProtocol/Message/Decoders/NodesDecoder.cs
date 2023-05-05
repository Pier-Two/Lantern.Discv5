using Lantern.Discv5.Enr.EnrFactory;
using Lantern.Discv5.Rlp;
using Lantern.Discv5.WireProtocol.Message.Responses;

namespace Lantern.Discv5.WireProtocol.Message.Decoders;

public class NodesDecoder : IMessageDecoder<NodesMessage>
{
    public NodesMessage DecodeMessage(byte[] message)
    {
        var rawMessage = message[1..];
        var decodedMessage = RlpDecoder.Decode(rawMessage);
        var requestId = decodedMessage[0];
        var total = RlpExtensions.ByteArrayToInt32(decodedMessage[1]);
        var enrs = new EnrRecordFactory().CreateFromMultipleEnrList(ExtractEnrRecord(decodedMessage.Skip(2).ToList(), total));
        return new NodesMessage(requestId, total, enrs);
    }
    
    private static IEnumerable<List<byte[]>> ExtractEnrRecord(IReadOnlyList<byte[]> data, int total)
    {
        var list = new List<List<byte[]>>(total);

        for (var i = 0; i < data.Count; i++)
        {
            // If the ENR uses a different identity scheme then the signature length could be different.
            // Add a check first if the identity scheme is v4, then check if the length is 64.
            if (data[i].Length != 64) continue;

            var subList = CreateSubList(data, i);
            list.Add(subList);
        }

        return list;
    }

    private static List<byte[]> CreateSubList(IReadOnlyList<byte[]> data, int startIndex)
    {
        var subList = new List<byte[]>();

        for (var j = startIndex; j < data.Count; j++)
        {
            if (startIndex != j && data[j].Length == 64)
                break;

            subList.Add(data[j]);
        }

        return subList;
    }
}