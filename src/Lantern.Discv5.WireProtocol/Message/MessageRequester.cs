using Lantern.Discv5.WireProtocol.Identity;
using Lantern.Discv5.WireProtocol.Message.Requests;
using Lantern.Discv5.WireProtocol.Table;
using Lantern.Discv5.WireProtocol.Utility;
using Microsoft.Extensions.Logging;

namespace Lantern.Discv5.WireProtocol.Message;

public class MessageRequester : IMessageRequester
{
    private readonly IIdentityManager _identityManager;
    private readonly IRoutingTable _routingTable;
    private readonly IRequestManager _requestManager;
    private readonly ITalkRequester? _talkRequester;
    private readonly ILogger<MessageRequester> _logger;
    private readonly Dictionary<byte[], CachedRequest> _cachedRequests;

    public MessageRequester(IIdentityManager identityManager, IRoutingTable routingTable, IRequestManager requestManager, ILoggerFactory loggerFactory, ITalkRequester? talkRequester = null)
    {
        _identityManager = identityManager;
        _routingTable = routingTable;
        _requestManager = requestManager;
        _talkRequester = talkRequester;
        _logger = loggerFactory.CreateLogger<MessageRequester>();
        _cachedRequests = new Dictionary<byte[], CachedRequest>(ByteArrayEqualityComparer.Instance);
    }

    public byte[]? ConstructPingMessage(byte[] destNodeId, bool isRequest = true)
    {
        _logger.LogInformation("Constructing message of type {MessageType}", MessageType.Ping);
        var pingMessage = new PingMessage((int)_identityManager.Record.SequenceNumber);

        if (isRequest)
        {
            var result = _requestManager.AddPendingRequest(pingMessage.RequestId, new PendingRequest(destNodeId, pingMessage));

            if(result == false)
            {
                _logger.LogWarning("Failed to add pending request. Request id: {RequestId}", Convert.ToHexString(pingMessage.RequestId));
                return null;
            }
        }
        else
        {
            if (!_cachedRequests.ContainsKey(destNodeId))
            {
                _cachedRequests.Add(destNodeId, new CachedRequest(destNodeId, pingMessage));
            }
        }
        
        _logger.LogDebug("Ping message constructed: {PingMessage}", pingMessage.RequestId);
        return pingMessage.EncodeMessage();
    }
    
    public byte[]? ConstructFindNodeMessage(byte[] destNodeId, byte[] targetNodeId, bool isRequest = true)
    {
        _logger.LogInformation("Constructing message of type {MessageType}", MessageType.FindNode);


        var distances = new[] { TableUtility.Log2Distance(destNodeId, targetNodeId) };
        var findNodesMessage = new FindNodeMessage(distances);

        if (isRequest)
        {
            var result = _requestManager.AddPendingRequest(findNodesMessage.RequestId, new PendingRequest(destNodeId, findNodesMessage));

            if(result == false)
            {
                _logger.LogWarning("Failed to add pending request. Request id: {RequestId}", Convert.ToHexString(findNodesMessage.RequestId));
                return null;
            }
        }
        else
        {
            if (!_cachedRequests.ContainsKey(destNodeId))
            {
                _cachedRequests.Add(destNodeId, new CachedRequest(destNodeId, findNodesMessage));
            }
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
            _cachedRequests.Add(destNodeId, new CachedRequest(destNodeId, talkReqMessage));
        }

        _logger.LogDebug("TalkReq message constructed: {TalkReqMessage}", talkReqMessage.RequestId);
        return talkReqMessage.EncodeMessage();
    }

    public byte[]? ConstructTalkRespMessage(byte[] message, bool isRequest = true)
    {
        throw new NotImplementedException();
    }
    
    public CachedRequest? GetCachedRequest(byte[] nodeId)
    {
        if (_cachedRequests.TryGetValue(nodeId, out var cachedRequest))
        {
            return cachedRequest;
        }

        return null;
    }

    public void RemoveCachedRequest(byte[] nodeId)
    {
        _cachedRequests.Remove(nodeId);
    }

    public byte[] CreateFromCachedRequest(CachedRequest request)
    {
        var pendingRequest = new PendingRequest(request.NodeId, request.Message);
        _requestManager.AddPendingRequest(request.Message.RequestId, pendingRequest);
        
        return request.Message.EncodeMessage();
    }
}