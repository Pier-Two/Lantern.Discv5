using System.Net;
using Lantern.Discv5.Enr.EnrFactory;
using Lantern.Discv5.Rlp;
using Lantern.Discv5.WireProtocol.Message.Requests;
using Lantern.Discv5.WireProtocol.Message.Responses;

namespace Lantern.Discv5.WireProtocol.Message;

public class MessageDecoder : IMessageDecoder
{
    public Message DecodeMessage(byte[] message)
    {
        var messageType = (MessageType)message[0];
        
        return messageType switch
        {
            MessageType.Ping => DecodePingMessage(message),
            MessageType.Pong => DecodePongMessage(message),
            MessageType.FindNode => DecodeFindNodeMessage(message),
            MessageType.Nodes => DecodeNodesMessage(message),
            MessageType.TalkReq => DecodeTalkReqMessage(message),
            MessageType.TalkResp => DecodeTalkRespMessage(message),
            MessageType.RegTopic => DecodeRegTopicMessage(message),
            MessageType.RegConfirmation => DecodeRegConfirmationMessage(message),
            MessageType.Ticket => DecodeTicketMessage(message),
            MessageType.TopicQuery => DecodeTopicQueryMessage(message),
            _ => throw new ArgumentOutOfRangeException(nameof(messageType), messageType, null)
        };
    }
    
    private static Message DecodePingMessage(byte[] message)
    {
        var decodedMessage = RlpDecoder.Decode(message[1..]);
        return new PingMessage(RlpExtensions.ByteArrayToInt32(decodedMessage[1]))
        {
            RequestId = decodedMessage[0]
        };
    }
    
    private static Message DecodePongMessage(byte[] message)
    {
        var decodedMessage = RlpDecoder.Decode(message[1..]);
        return new PongMessage(decodedMessage[0], (int)RlpExtensions.ByteArrayToInt64(decodedMessage[1]),
            new IPAddress(decodedMessage[2]), RlpExtensions.ByteArrayToInt32(decodedMessage[3]));
    }
    
    private static Message DecodeFindNodeMessage(byte[] message)
    {
        var decodedMessage = RlpDecoder.Decode(message[1..]);
        var distances = new List<int>(decodedMessage.Count - 1);

        for (var i = 1; i < decodedMessage.Count; i++)
            distances.Add(RlpExtensions.ByteArrayToInt32(decodedMessage[i]));

        return new FindNodeMessage(distances.ToArray())
        {
            RequestId = decodedMessage[0]
        };
    }

    private static Message DecodeNodesMessage(byte[] message)
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
    
    private static Message DecodeTalkReqMessage(byte[] message)
    {
        var decodedMessage = RlpDecoder.Decode(message[1..]);
        return new TalkReqMessage(decodedMessage[1], decodedMessage[2])
        {
            RequestId = decodedMessage[0]
        };
    }
    
    private static Message DecodeTalkRespMessage(byte[] message)
    {
        var decodedMessage = RlpDecoder.Decode(message[1..]);
        return new TalkRespMessage(decodedMessage[0], decodedMessage[1]);
    }
    
    private static Message DecodeRegTopicMessage(byte[] message)
    {
        var decodedMessage = RlpDecoder.Decode(message[1..]);
        var decodedTopic = decodedMessage[1];
        var decodedEnr = decodedMessage.GetRange(2, decodedMessage.Count - 2).SkipLast(1).ToList();
        var enr = new EnrRecordFactory().CreateFromDecoded(decodedEnr.ToList());
        var decodedTicket = decodedMessage.Last();
        return new RegTopicMessage(decodedTopic, enr, decodedTicket)
        {
            RequestId = decodedMessage[0]
        };
    }
    
    private static Message DecodeRegConfirmationMessage(byte[] message)
    {
        var decodedMessage = RlpDecoder.Decode(message[1..]);
        return new RegConfirmationMessage(decodedMessage[0], decodedMessage[1]);
    }
    
    private static Message DecodeTicketMessage(byte[] message)
    {
        var decodedMessage = RlpDecoder.Decode(message[1..]);
        return new TicketMessage(decodedMessage[0], decodedMessage[1],
            RlpExtensions.ByteArrayToInt32(decodedMessage[2]));
    } 
    
    private static Message DecodeTopicQueryMessage(byte[] message)
    {
        var decodedMessage = RlpDecoder.Decode(message[1..]);
        return new TopicQueryMessage(decodedMessage[1])
        {
            RequestId = decodedMessage[0]
        };
    }
}