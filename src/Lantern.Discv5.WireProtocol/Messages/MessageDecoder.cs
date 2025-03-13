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
        var decodedMessage = RlpDecoder.Decode(message.AsMemory(1));
        return new PingMessage(RlpExtensions.ByteArrayToInt32(decodedMessage[1].GetData()))
        {
            RequestId = decodedMessage[0].GetData()
        };
    }

    private static PongMessage DecodePongMessage(byte[] message)
    {
        var decodedMessage = RlpDecoder.Decode(message.AsMemory(1));
        return new PongMessage(decodedMessage[0].GetData(), (int)RlpExtensions.ByteArrayToInt64(decodedMessage[1].GetData()),
            new IPAddress(decodedMessage[2].GetData()), RlpExtensions.ByteArrayToInt32(decodedMessage[3].GetData()));
    }

    private static FindNodeMessage DecodeFindNodeMessage(byte[] message)
    {
        var decodedMessage = RlpDecoder.Decode(message.AsMemory(1));
        var distanceRlps = RlpDecoder.Decode(decodedMessage[1].InnerSpan);
        var distances = new int[distanceRlps.Length];

        for (var i = 0; i < distanceRlps.Length; i++)
            distances[i] = RlpExtensions.ByteArrayToInt32(decodedMessage[i].GetData());

        return new FindNodeMessage(distances)
        {
            RequestId = decodedMessage[0].GetData()
        };
    }

    private NodesMessage DecodeNodesMessage(byte[] message)
    {
        var decodedMessage = RlpDecoder.Decode(message.AsMemory(1));
        var requestId = decodedMessage[0].GetData();
        var total = RlpExtensions.ByteArrayToInt32(decodedMessage[1].GetData());
        var encodedEnrs = RlpDecoder.Decode(decodedMessage[2].InnerSpan);
        var enrs = enrFactory.CreateFromMultipleEnrList(encodedEnrs, identityManager.Verifier);
        return new NodesMessage(requestId, total, enrs);
    }

    private static TalkReqMessage DecodeTalkReqMessage(byte[] message)
    {
        var decodedMessage = RlpDecoder.Decode(message.AsMemory(1));
        return new TalkReqMessage(decodedMessage[1].GetData(), decodedMessage[2].GetData())
        {
            RequestId = decodedMessage[0].GetData()
        };
    }

    private static TalkRespMessage DecodeTalkRespMessage(byte[] message)
    {
        var decodedMessage = RlpDecoder.Decode(message.AsMemory(1));
        return new TalkRespMessage(decodedMessage[0].GetData(), decodedMessage[1].GetData());
    }

    private RegTopicMessage DecodeRegTopicMessage(byte[] message)
    {
        var decodedMessage = RlpDecoder.Decode(message.AsMemory(1));
        var decodedTopic = decodedMessage[1].GetData();
        var enr = enrFactory.CreateFromRlp(decodedMessage[2], identityManager.Verifier);
        var decodedTicket = decodedMessage[3].GetData();
        return new RegTopicMessage(decodedTopic, enr, decodedTicket)
        {
            RequestId = decodedMessage[0].GetData()
        };
    }

    private static RegConfirmationMessage DecodeRegConfirmationMessage(byte[] message)
    {
        var decodedMessage = RlpDecoder.Decode(message.AsMemory(1));
        return new RegConfirmationMessage(decodedMessage[0].GetData(), decodedMessage[1].GetData());
    }

    private static TicketMessage DecodeTicketMessage(byte[] message)
    {
        var decodedMessage = RlpDecoder.Decode(message.AsMemory(1));
        return new TicketMessage(decodedMessage[0].GetData(), decodedMessage[1].GetData(),
            RlpExtensions.ByteArrayToInt32(decodedMessage[2].GetData()));
    }

    private static TopicQueryMessage DecodeTopicQueryMessage(byte[] message)
    {
        var decodedMessage = RlpDecoder.Decode(message.AsMemory(1));
        return new TopicQueryMessage(decodedMessage[1].GetData())
        {
            RequestId = decodedMessage[0].GetData()
        };
    }
}