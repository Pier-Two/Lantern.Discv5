using Lantern.Discv5.WireProtocol.Identity;
using Lantern.Discv5.WireProtocol.Message.Requests;
using Lantern.Discv5.WireProtocol.Packet;
using Lantern.Discv5.WireProtocol.Table;
using Lantern.Discv5.WireProtocol.Utility;
using Microsoft.Extensions.Logging;

namespace Lantern.Discv5.WireProtocol.Message;

public class MessageRequester : IMessageRequester
{
    private readonly IIdentityManager _identityManager;
    private readonly IRoutingTable _routingTable;
    private readonly IPendingRequests _pendingRequests;
    private readonly ITalkRequester? _talkRequester;
    private readonly ILogger<MessageRequester> _logger;

    public MessageRequester(IIdentityManager identityManager, IRoutingTable routingTable, IPendingRequests pendingRequests, ILoggerFactory loggerFactory, ITalkRequester? talkRequester = null)
    {
        _identityManager = identityManager;
        _routingTable = routingTable;
        _pendingRequests = pendingRequests;
        _talkRequester = talkRequester;
        _logger = loggerFactory.CreateLogger<MessageRequester>();
    }
    
    public byte[] ConstructMessage(MessageType messageType, byte[] destNodeId)
    {
        _logger.LogInformation("Constructing message of type {MessageType}", messageType);
        
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
            _logger.LogWarning("Failed to add pending request. Request id: {RequestId}", Convert.ToHexString(pingMessage.RequestId));
        }
        
        _logger.LogDebug("Ping message constructed: {PingMessage}", pingMessage.RequestId);
        return pingMessage.EncodeMessage();
    }
    
    private byte[] ConstructFindNodeMessage(byte[] destNodeId)
    {
        var randomNodeId = RandomUtility.GenerateNodeId(PacketConstants.NodeIdSize);
        var distances = _routingTable.GetClosestNeighbours(randomNodeId);
        var findNodesMessage = new FindNodeMessage(distances);
        var result = _pendingRequests.AddPendingRequest(findNodesMessage.RequestId, new PendingRequest(destNodeId, findNodesMessage));

        if(result == false)
        {
            _logger.LogWarning("Failed to add pending request. Request id: {RequestId}", Convert.ToHexString(findNodesMessage.RequestId));
        }
        
        _logger.LogDebug("FindNode message constructed: {FindNodeMessage}", findNodesMessage.RequestId);
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
            _logger.LogWarning("Failed to add pending request. Request id: {RequestId}", Convert.ToHexString(talkReqMessage.RequestId));
        }

        _logger.LogDebug("TalkReq message constructed: {TalkReqMessage}", talkReqMessage.RequestId);
        return talkReqMessage.EncodeMessage();
    }
}