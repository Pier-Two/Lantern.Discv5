using System.Net;
using Lantern.Discv5.Enr;
using Lantern.Discv5.Rlp;
using Lantern.Discv5.WireProtocol.Identity;
using Lantern.Discv5.WireProtocol.Messages.Requests;
using Lantern.Discv5.WireProtocol.Messages.Responses;

namespace Lantern.Discv5.WireProtocol.Messages;

public class MessageDecoder(IIdentityManager identityManager, IEnrFactory enrFactory) : IMessageDecoder
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
            _ => throw new ArgumentOutOfRangeException()
        };
    }
    
    private static PingMessage DecodePingMessage(byte[] message)
    {
        var decodedMessage = RlpDecoder.Decode(message[1..]);
        return new PingMessage(RlpExtensions.ByteArrayToInt32(decodedMessage[1]))
        {
            RequestId = decodedMessage[0]
        };
    }
    
    private static PongMessage DecodePongMessage(byte[] message)
    {
        var decodedMessage = RlpDecoder.Decode(message[1..]);
        return new PongMessage(decodedMessage[0], (int)RlpExtensions.ByteArrayToInt64(decodedMessage[1]),
            new IPAddress(decodedMessage[2]), RlpExtensions.ByteArrayToInt32(decodedMessage[3]));
    }
    
    private static FindNodeMessage DecodeFindNodeMessage(byte[] message)
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

    private NodesMessage DecodeNodesMessage(byte[] message)
    {
        var rawMessage = message[1..];
        var decodedMessage = RlpDecoder.Decode(rawMessage);
        var requestId = decodedMessage[0];
        var total = RlpExtensions.ByteArrayToInt32(decodedMessage[1]);
        var enrs = enrFactory.CreateFromMultipleEnrList(ExtractEnrRecord(decodedMessage.Skip(2).ToList(), total),
            identityManager.Verifier);
        return new NodesMessage(requestId, total, enrs);
    }
    
    private static TalkReqMessage DecodeTalkReqMessage(byte[] message)
    {
        var decodedMessage = RlpDecoder.Decode(message[1..]);
        return new TalkReqMessage(decodedMessage[1], decodedMessage[2])
        {
            RequestId = decodedMessage[0]
        };
    }
    
    private static TalkRespMessage DecodeTalkRespMessage(byte[] message)
    {
        var decodedMessage = RlpDecoder.Decode(message[1..]);
        return new TalkRespMessage(decodedMessage[0], decodedMessage[1]);
    }
    
    private RegTopicMessage DecodeRegTopicMessage(byte[] message)
    {
        var decodedMessage = RlpDecoder.Decode(message[1..]);
        var decodedTopic = decodedMessage[1];
        var decodedEnr = decodedMessage.GetRange(2, decodedMessage.Count - 2).SkipLast(1).ToList();
        var enr = enrFactory.CreateFromDecoded(decodedEnr.ToList(), identityManager.Verifier);
        var decodedTicket = decodedMessage.Last();
        return new RegTopicMessage(decodedTopic, enr, decodedTicket)
        {
            RequestId = decodedMessage[0]
        };
    }
    
    private static RegConfirmationMessage DecodeRegConfirmationMessage(byte[] message)
    {
        var decodedMessage = RlpDecoder.Decode(message[1..]);
        return new RegConfirmationMessage(decodedMessage[0], decodedMessage[1]);
    }
    
    private static TicketMessage DecodeTicketMessage(byte[] message)
    {
        var decodedMessage = RlpDecoder.Decode(message[1..]);
        return new TicketMessage(decodedMessage[0], decodedMessage[1],
            RlpExtensions.ByteArrayToInt32(decodedMessage[2]));
    } 
    
    private static TopicQueryMessage DecodeTopicQueryMessage(byte[] message)
    {
        var decodedMessage = RlpDecoder.Decode(message[1..]);
        return new TopicQueryMessage(decodedMessage[1])
        {
            RequestId = decodedMessage[0]
        };
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