using Lantern.Discv5.WireProtocol.Identity;
using Lantern.Discv5.WireProtocol.Message.Requests;
using Lantern.Discv5.WireProtocol.Table;
using Lantern.Discv5.WireProtocol.Utility;
using Microsoft.Extensions.Logging;

namespace Lantern.Discv5.WireProtocol.Message;

public class MessageRequester : IMessageRequester
{
    private readonly IIdentityManager _identityManager;
    private readonly IRequestManager _requestManager;
    private readonly ITalkRequester? _talkRequester;
    private readonly ILogger<MessageRequester> _logger;
    public MessageRequester(IIdentityManager identityManager, IRequestManager requestManager, ILoggerFactory loggerFactory, ITalkRequester? talkRequester = null)
    {
        _identityManager = identityManager;
        _requestManager = requestManager;
        _talkRequester = talkRequester;
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
    
    public byte[]? ConstructFindNodeMessage(byte[] destNodeId, byte[] targetNodeId, bool varyDistance)
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
    
    public byte[] ConstructCachedFindNodeMessage(byte[] destNodeId, byte[] targetNodeId, bool varyDistance)
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

    public byte[]? ConstructTalkReqMessage(byte[] destNodeId, bool isRequest = true)
    {
        _logger.LogInformation("Constructing message of type {MessageType}", MessageType.TalkReq);
        
        if(_talkRequester == null)
        {
            _logger.LogError("Talk requester is null");
            return null;
        }
        
        var protocol = _talkRequester.GetProtocol();
        var request = _talkRequester.GetTalkRequest();
        var talkReqMessage = new TalkReqMessage(protocol, request);

        if (isRequest)
        {
            var result = _requestManager.AddPendingRequest(talkReqMessage.RequestId, new PendingRequest(destNodeId, talkReqMessage));
        
            if(result == false)
            {
                _logger.LogWarning("Failed to add pending request. Request id: {RequestId}", Convert.ToHexString(talkReqMessage.RequestId));
                return null;
            }
        }
        else
        {
            _requestManager.AddCachedRequest(destNodeId, new CachedRequest(destNodeId, talkReqMessage));
        }

        _logger.LogDebug("TalkReq message constructed: {TalkReqMessage}", talkReqMessage.RequestId);
        return talkReqMessage.EncodeMessage();
    }

    public byte[]? ConstructTalkRespMessage(byte[] message, bool isRequest = true)
    {
        throw new NotImplementedException();
    }
}