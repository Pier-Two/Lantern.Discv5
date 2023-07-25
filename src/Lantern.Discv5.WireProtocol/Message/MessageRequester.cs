using Lantern.Discv5.WireProtocol.Identity;
using Lantern.Discv5.WireProtocol.Message.Requests;
using Lantern.Discv5.WireProtocol.Message.Responses;
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
        var pendingRequest = new PendingRequest(destNodeId, pingMessage);
        var result = _requestManager.AddPendingRequest(pingMessage.RequestId, pendingRequest);
        
        if(!result)
        {
            _logger.LogWarning("Failed to add pending request. Request id: {RequestId}", Convert.ToHexString(pingMessage.RequestId));
            return null;
        }

        _logger.LogDebug("Ping message constructed: {PingMessage}", pingMessage.RequestId);
        return pingMessage.EncodeMessage();
    }

    public byte[]? ConstructCachedPingMessage(byte[] destNodeId)
    {
        _logger.LogInformation("Constructing message of type {MessageType}", MessageType.Ping);
        var pingMessage = new PingMessage((int)_identityManager.Record.SequenceNumber);
        var cachedRequest = new CachedRequest(destNodeId, pingMessage);
        var result = _requestManager.AddCachedRequest(destNodeId, cachedRequest);
        
        if(!result)
        {
            _logger.LogWarning("Failed to add cached request. Request id: {RequestId}", Convert.ToHexString(destNodeId));
            return null;
        }
        
        _logger.LogDebug("Ping message constructed: {PingMessage}", pingMessage.RequestId);
        return pingMessage.EncodeMessage();
    }
    
    public byte[]? ConstructFindNodeMessage(byte[] destNodeId, byte[] targetNodeId)
    {
        var distance = TableUtility.Log2Distance(destNodeId, targetNodeId);
        var distances = new[] { distance };
        
        _logger.LogInformation("Constructing message of type {MessageType} at distances {Distances}", MessageType.FindNode, string.Join(", ", distances.Select(d => d.ToString())));

        var findNodesMessage = new FindNodeMessage(distances);
        var pendingRequest = new PendingRequest(destNodeId, findNodesMessage);
        var result = _requestManager.AddPendingRequest(findNodesMessage.RequestId, pendingRequest);

        if(!result)
        {
            _logger.LogWarning("Failed to add pending request. Request id: {RequestId}", Convert.ToHexString(findNodesMessage.RequestId));
            return null;
        }

        _logger.LogDebug("FindNode message constructed: {FindNodeMessage}", findNodesMessage.RequestId);
        return findNodesMessage.EncodeMessage();
    }
    
    public byte[]? ConstructCachedFindNodeMessage(byte[] destNodeId, byte[] targetNodeId)
    {
        var distance = TableUtility.Log2Distance(destNodeId, targetNodeId);
        var distances = new[] { distance };
        
        _logger.LogInformation("Constructing message of type {MessageType} at distances {Distances}", MessageType.FindNode, string.Join(", ", distances.Select(d => d.ToString())));

        var findNodesMessage = new FindNodeMessage(distances);
        var cachedRequest = new CachedRequest(destNodeId, findNodesMessage);
        var result = _requestManager.AddCachedRequest(destNodeId, cachedRequest);
        
        if(!result)
        {
            _logger.LogWarning("Failed to add cached request. Request id: {RequestId}", Convert.ToHexString(destNodeId));
            return null;
        }

        _logger.LogDebug("FindNode message constructed: {FindNodeMessage}", findNodesMessage.RequestId);
        return findNodesMessage.EncodeMessage();
    }

    public byte[]? ConstructTalkReqMessage(byte[] destNodeId, byte[] protocol, byte[] request)
    {
        _logger.LogInformation("Constructing message of type {MessageType}", MessageType.TalkReq);

        var talkReqMessage = new TalkReqMessage(protocol, request);
        var pendingRequest = new PendingRequest(destNodeId, talkReqMessage);
        var result = _requestManager.AddPendingRequest(talkReqMessage.RequestId, pendingRequest);
        
        if(!result)
        {
            _logger.LogWarning("Failed to add pending request. Request id: {RequestId}", Convert.ToHexString(talkReqMessage.RequestId));
            return null;
        }

        _logger.LogDebug("TalkReq message constructed: {TalkReqMessage}", talkReqMessage.RequestId);
        return talkReqMessage.EncodeMessage();
    }
    
    public byte[]? ConstructTalkRespMessage(byte[] destNodeId, byte[] response)
    {
        _logger.LogInformation("Constructing message of type {MessageType}", MessageType.TalkResp);
        
        var talkRespMessage = new TalkRespMessage(response);
        var pendingRequest = new PendingRequest(destNodeId, talkRespMessage);
        var result = _requestManager.AddPendingRequest(talkRespMessage.RequestId, pendingRequest);
        
        if(!result)
        {
            _logger.LogWarning("Failed to add pending request. Request id: {RequestId}", Convert.ToHexString(talkRespMessage.RequestId));
            return null;
        }
        
        _logger.LogDebug("TalkResp message constructed: {TalkRespMessage}", talkRespMessage.RequestId);
        return talkRespMessage.EncodeMessage();
    }
    
    public byte[]? ConstructCachedTalkReqMessage(byte[] destNodeId, byte[] protocol, byte[] request)
    {
        _logger.LogInformation("Constructing message of type {MessageType}", MessageType.TalkReq);
        
        var talkReqMessage = new TalkReqMessage(protocol, request);
        var cachedRequest = new CachedRequest(destNodeId, talkReqMessage);
        var result = _requestManager.AddCachedRequest(destNodeId, cachedRequest);
        
        if(!result)
        {
            _logger.LogWarning("Failed to add cached request. Request id: {RequestId}", Convert.ToHexString(destNodeId));
            return null;
        }
        
        _logger.LogDebug("TalkReq message constructed: {TalkReqMessage}", talkReqMessage.RequestId);
        return talkReqMessage.EncodeMessage();
    }

    public byte[]? ConstructCachedTalkRespMessage(byte[] destNodeId, byte[] response)
    {
        _logger.LogInformation("Constructing message of type {MessageType}", MessageType.TalkResp);
        
        var talkRespMessage = new TalkRespMessage(response);
        var cachedRequest = new CachedRequest(destNodeId, talkRespMessage);
        var result = _requestManager.AddCachedRequest(destNodeId, cachedRequest);
        
        if(!result)
        {
            _logger.LogWarning("Failed to add cached request. Request id: {RequestId}", Convert.ToHexString(destNodeId));
            return null;
        }

        _logger.LogDebug("TalkResp message constructed: {TalkRespMessage}", talkRespMessage.RequestId);
        return talkRespMessage.EncodeMessage();
    }
}