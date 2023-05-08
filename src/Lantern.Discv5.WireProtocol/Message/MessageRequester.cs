using Lantern.Discv5.WireProtocol.Identity;
using Lantern.Discv5.WireProtocol.Message.Requests;
using Lantern.Discv5.WireProtocol.Packet;
using Lantern.Discv5.WireProtocol.Table;

namespace Lantern.Discv5.WireProtocol.Message;

public class MessageRequester : IMessageRequester
{
    private readonly IIdentityManager _identityManager;
    private readonly ITableManager _tableManager;
    private readonly IPendingRequests _pendingRequests;
    private readonly ITalkRequester? _talkRequester;

    public MessageRequester(IIdentityManager identityManager, ITableManager tableManager, IPendingRequests pendingRequests, ITalkRequester? talkRequester = null)
    {
        _identityManager = identityManager;
        _tableManager = tableManager;
        _pendingRequests = pendingRequests;
        _talkRequester = talkRequester;
    }
    
    public byte[] ConstructMessage(MessageType messageType, byte[] destNodeId)
    {
        return messageType switch
        {
            MessageType.Ping => ConstructPingMessage(destNodeId),
            MessageType.FindNode => ConstructFindNodeMessage(destNodeId),
            MessageType.TalkReq => ConstructTalkReqMessage(destNodeId),
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

    private byte[] ConstructTalkReqMessage(byte[] destNodeId)
    {
        if(_talkRequester == null)
        {
            throw new Exception("Talk requester is null");
        }
        
        var protocol = _talkRequester.GetProtocol();
        var request = _talkRequester.GetTalkRequest();
        var talkReqMessage = new TalkReqMessage(protocol, request);
        var result = _pendingRequests.AddPendingRequest(talkReqMessage.RequestId, new PendingRequest(destNodeId, talkReqMessage));
        
        if(result == false)
        {
            Console.WriteLine("Failed to add pending request. Request id: " + Convert.ToHexString(talkReqMessage.RequestId));
        }

        return talkReqMessage.EncodeMessage();
    }
}