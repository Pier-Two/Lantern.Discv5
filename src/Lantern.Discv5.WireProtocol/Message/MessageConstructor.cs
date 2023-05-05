using Lantern.Discv5.WireProtocol.Identity;
using Lantern.Discv5.WireProtocol.Message.Requests;
using Lantern.Discv5.WireProtocol.Packet;
using Lantern.Discv5.WireProtocol.Packet.Headers;
using Lantern.Discv5.WireProtocol.Table;

namespace Lantern.Discv5.WireProtocol.Message;

public class MessageConstructor : IMessageConstructor
{
    private readonly IIdentityManager _identityManager;
    private readonly ITableManager _tableManager;
    private readonly IPendingRequests _pendingRequests;

    public MessageConstructor(IIdentityManager identityManager, ITableManager tableManager, IPendingRequests pendingRequests)
    {
        _identityManager = identityManager;
        _tableManager = tableManager;
        _pendingRequests = pendingRequests;
    }
    
    public byte[]? ConstructMessage(MessageType messageType, byte[] destNodeId)
    {
        return messageType switch
        {
            MessageType.Ping => ConstructPingMessage(destNodeId),
            MessageType.Pong => ConstructPongMessage(),
            MessageType.FindNode => ConstructFindNodeMessage(destNodeId),
            MessageType.Nodes => ConstructNodesMessage(),
            MessageType.TalkReq => ConstructTalkReqMessage(),
            MessageType.TalkResp => ConstructTalkRespMessage(),
            MessageType.RegTopic => ConstructRegTopicMessage(),
            MessageType.Ticket => ConstructTicketMessage(),
            MessageType.RegConfirmation => ConstructRegConfirmationMessage(),
            MessageType.TopicQuery => ConstructTopicQueryMessage(),
            _ => throw new ArgumentOutOfRangeException(nameof(messageType), messageType, null)
        };
    }
    
    private byte[] ConstructPingMessage(byte[] destNodeId)
    {
        var pingMessage = new PingMessage((int)_identityManager.Record.SequenceNumber);
        var result = _pendingRequests.AddPendingRequest(pingMessage.RequestId, new PendingRequest(destNodeId, pingMessage));

        if(result == false)
        {
            Console.WriteLine("Failed to add pending request. Request id: " + Convert.ToHexString(pingMessage.RequestId));
        }
        
        return pingMessage.EncodeMessage();
    }
    
    private byte[] ConstructPongMessage()
    {
        throw new NotImplementedException();
    }
    
    private byte[] ConstructFindNodeMessage(byte[] destNodeId)
    {
        var randomNodeId = RandomUtility.GenerateNodeId(PacketConstants.NodeIdSize);
        var distances = _tableManager.GetClosestNeighbours(randomNodeId);
        var findNodesMessage = new FindNodeMessage(distances);
        var result = _pendingRequests.AddPendingRequest(findNodesMessage.RequestId, new PendingRequest(destNodeId, findNodesMessage));

        if(result == false)
        {
            Console.WriteLine("Failed to add pending request. Request id: " + Convert.ToHexString(findNodesMessage.RequestId));
        }

        return findNodesMessage.EncodeMessage();
    }
    
    private byte[] ConstructNodesMessage()
    {
        throw new NotImplementedException();
    }
    
    private byte[] ConstructTalkReqMessage()
    {
        throw new NotImplementedException();
    }
    
    private byte[] ConstructTalkRespMessage()
    {
        throw new NotImplementedException();
    }
    
    private byte[] ConstructRegTopicMessage()
    {
        throw new NotImplementedException();
    }

    private byte[] ConstructTicketMessage()
    {
        throw new NotImplementedException();
    }
    
    private byte[] ConstructRegConfirmationMessage()
    {
        throw new NotImplementedException();
    }
    
    private byte[] ConstructTopicQueryMessage()
    {
        throw new NotImplementedException();
    }
}