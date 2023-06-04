using Lantern.Discv5.WireProtocol.Identity;
using Lantern.Discv5.WireProtocol.Message.Requests;
using Lantern.Discv5.WireProtocol.Table;
using Microsoft.Extensions.Logging;

namespace Lantern.Discv5.WireProtocol.Message;

public class MessageRequester : IMessageRequester
{
    private readonly IIdentityManager _identityManager;
    private readonly IRequestManager _requestManager;
    private readonly ILogger<MessageRequester> _logger;
    
    public MessageRequester(IIdentityManager identityManager, IRequestManager requestManager, ILoggerFactory loggerFactory)
    {
        _identityManager = identityManager;
        _requestManager = requestManager;
        _logger = loggerFactory.CreateLogger<MessageRequester>();
    }

    public byte[]? ConstructPingMessage(byte[] destNodeId)
    {
        _logger.LogInformation("Constructing message of type {MessageType}", MessageType.Ping);
        var pingMessage = new PingMessage((int)_identityManager.Record.SequenceNumber);
        
        var result = _requestManager.AddPendingRequest(pingMessage.RequestId, new PendingRequest(destNodeId, pingMessage));

        if(result == false)
        {
            _logger.LogWarning("Failed to add pending request. Request id: {RequestId}", Convert.ToHexString(pingMessage.RequestId));
            return null;
        }

        _logger.LogDebug("Ping message constructed: {PingMessage}", pingMessage.RequestId);
        return pingMessage.EncodeMessage();
    }

    public byte[] ConstructCachedPingMessage(byte[] destNodeId)
    {
        _logger.LogInformation("Constructing message of type {MessageType}", MessageType.Ping);
        var pingMessage = new PingMessage((int)_identityManager.Record.SequenceNumber);
        var cachedRequest = new CachedRequest(destNodeId, pingMessage);
        
        if (!_requestManager.ContainsCachedRequest(destNodeId))
        {
            _logger.LogDebug("Adding cached request for node {NodeId}", Convert.ToHexString(destNodeId));
            _requestManager.AddCachedRequest(destNodeId, cachedRequest);
        }

        _logger.LogDebug("Ping message constructed: {PingMessage}", pingMessage.RequestId);
        return pingMessage.EncodeMessage();
    }
    
    public byte[]? ConstructFindNodeMessage(byte[] destNodeId, byte[] targetNodeId)
    {
        var distance = TableUtility.Log2Distance(destNodeId, targetNodeId);
        var distances = new[] { distance };
        var findNodesMessage = new FindNodeMessage(distances);
        
        _logger.LogInformation("Constructing message of type {MessageType} at distances {Distances}", MessageType.FindNode, string.Join(", ", distances.Select(d => d.ToString())));

        var result = _requestManager.AddPendingRequest(findNodesMessage.RequestId, new PendingRequest(destNodeId, findNodesMessage));

        if(result == false)
        {
            _logger.LogWarning("Failed to add pending request. Request id: {RequestId}", Convert.ToHexString(findNodesMessage.RequestId));
            return null;
        }

        _logger.LogDebug("FindNode message constructed: {FindNodeMessage}", findNodesMessage.RequestId);
        return findNodesMessage.EncodeMessage();
    }
    
    public byte[] ConstructCachedFindNodeMessage(byte[] destNodeId, byte[] targetNodeId)
    {
        var distance = TableUtility.Log2Distance(destNodeId, targetNodeId);
        var distances = new[] { distance };
        var findNodesMessage = new FindNodeMessage(distances);
        var cachedRequest = new CachedRequest(destNodeId, findNodesMessage);
        
        _logger.LogInformation("Constructing message of type {MessageType} at distances {Distances}", MessageType.FindNode, string.Join(", ", distances.Select(d => d.ToString())));
        
        if (!_requestManager.ContainsCachedRequest(destNodeId))
        {
            _logger.LogInformation("Adding cached request for node {NodeId}", Convert.ToHexString(destNodeId));
            _requestManager.AddCachedRequest(destNodeId, cachedRequest);
        }

        _logger.LogDebug("FindNode message constructed: {FindNodeMessage}", findNodesMessage.RequestId);
        return findNodesMessage.EncodeMessage();
    }

    public byte[]? ConstructTalkReqMessage(byte[] destNodeId, byte[] protocol, byte[] request)
    {
        _logger.LogInformation("Constructing message of type {MessageType}", MessageType.TalkReq);

        var talkReqMessage = new TalkReqMessage(protocol, request);
        var result = _requestManager.AddPendingRequest(talkReqMessage.RequestId, new PendingRequest(destNodeId, talkReqMessage));
        
        if(result == false)
        {
            _logger.LogWarning("Failed to add pending request. Request id: {RequestId}", Convert.ToHexString(talkReqMessage.RequestId));
            return null;
        }
        
        _logger.LogDebug("TalkReq message constructed: {TalkReqMessage}", talkReqMessage.RequestId);
        return talkReqMessage.EncodeMessage();
    }

    public byte[]? ConstructCachedTalkReqMessage(byte[] destNodeId, byte[] protocol, byte[] request)
    {
        _logger.LogInformation("Constructing message of type {MessageType}", MessageType.TalkReq);
        
        var talkReqMessage = new TalkReqMessage(protocol, request);
        
        if(!_requestManager.ContainsCachedRequest(destNodeId))
        {
            _logger.LogInformation("Adding cached request for node {NodeId}", Convert.ToHexString(destNodeId));
            _requestManager.AddCachedRequest(destNodeId, new CachedRequest(destNodeId, talkReqMessage));
        }
        
        _logger.LogDebug("TalkReq message constructed: {TalkReqMessage}", talkReqMessage.RequestId);
        return talkReqMessage.EncodeMessage();
    }
}