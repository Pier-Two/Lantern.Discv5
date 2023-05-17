using System.Net;
using Lantern.Discv5.Enr.IdentityScheme.V4;
using Lantern.Discv5.WireProtocol.Identity;
using Lantern.Discv5.WireProtocol.Message.Requests;
using Lantern.Discv5.WireProtocol.Message.Responses;
using Lantern.Discv5.WireProtocol.Table;
using Microsoft.Extensions.Logging;

namespace Lantern.Discv5.WireProtocol.Message;

public class MessageResponder : IMessageResponder
{
    private const int RecordLimit = 16;
    private readonly IIdentityManager _identityManager;
    private readonly IRoutingTable _routingTable;
    private readonly IPendingRequests _pendingRequests;
    private readonly ILookupManager _lookupManager;
    private readonly ITalkResponder? _talkResponder;
    private readonly IMessageDecoder _messageDecoder;
    private readonly ILogger<MessageResponder> _logger;

    public MessageResponder(IIdentityManager identityManager, IRoutingTable routingTable, IPendingRequests pendingRequests, ILookupManager lookupManager, IMessageDecoder messageDecoder, ILoggerFactory loggerFactory, ITalkResponder? talkResponder = null)
    {
        _identityManager = identityManager;
        _routingTable = routingTable;
        _pendingRequests = pendingRequests;
        _lookupManager = lookupManager;
        _messageDecoder = messageDecoder;
        _talkResponder = talkResponder;
        _logger = loggerFactory.CreateLogger<MessageResponder>();
    }
    
    public byte[]? HandleMessage(byte[] message, IPEndPoint endPoint)
    {
        var messageType = (MessageType)message[0];

        return messageType switch
        {
            MessageType.Ping => HandlePingMessage(message, endPoint),
            MessageType.Pong => HandlePongMessage(message),
            MessageType.FindNode => HandleFindNodeMessage(message),
            MessageType.Nodes => HandleNodesMessage(message),
            MessageType.TalkReq => HandleTalkReqMessage(message),
            MessageType.TalkResp => HandleTalkRespMessage(message),
            _ => throw new ArgumentOutOfRangeException(nameof(messageType), messageType, null)
        };
    }
    
    private byte[] HandlePingMessage(byte[] message, IPEndPoint endPoint)
    {
        _logger.LogInformation("Received message type => {MessageType}", MessageType.Ping);
        var decodedMessage = _messageDecoder.DecodeMessage(message);
        var localEnrSeq = _identityManager.Record.SequenceNumber;
        var pongMessage = new PongMessage(decodedMessage.RequestId, (int)localEnrSeq, endPoint.Address, endPoint.Port);
        return pongMessage.EncodeMessage();
    }
    
    private byte[]? HandlePongMessage(byte[] message)
    {
        _logger.LogInformation("Received message type => {MessageType}", MessageType.Pong);
        var decodedMessage = (PongMessage)_messageDecoder.DecodeMessage(message);
        var result = _pendingRequests.ContainsPendingRequest(decodedMessage.RequestId);
        
        if (result == false)
        {
            _logger.LogWarning("Received pong message with unknown request id. Request id: {RequestId}",Convert.ToHexString(decodedMessage.RequestId));
            return null;
        }

        var pendingRequest = ValidateRequest(decodedMessage);
        
        if(pendingRequest == null)
            return null;
        
        var nodeEntry = _routingTable.GetNodeEntry(pendingRequest.NodeId);

        if (nodeEntry == null)
        {
            _logger.LogWarning("ENR record is not known. Cannot handle PONG message from node. ENR: {Record}", Convert.ToHexString(pendingRequest.NodeId));
            return null;
        }
        
        var enrRecord = nodeEntry.Record;
        
        if (nodeEntry.IsLive == false)
        {
            _routingTable.UpdateTable(enrRecord);
            _pendingRequests.RemovePendingRequest(decodedMessage.RequestId);

            _logger.LogInformation("Added bootstrap enr record to table. ENR: {Record}",Convert.ToHexString(new IdentitySchemeV4Verifier().GetNodeIdFromRecord(enrRecord)));

            if (!_identityManager.IsIpAddressAndPortSet())
            {
                var endpoint = new IPEndPoint(decodedMessage.RecipientIp, decodedMessage.RecipientPort);
                _identityManager.UpdateIpAddressAndPort(endpoint);
            }

            return null;
        }
        
        _pendingRequests.RemovePendingRequest(decodedMessage.RequestId);

        if (decodedMessage.EnrSeq <= (int)enrRecord!.SequenceNumber) 
            return null;
        
        var distance = new []{ 0 };
        var findNodeMessage = new FindNodeMessage(distance);
        
        return findNodeMessage.EncodeMessage();
    }
    
    private byte[] HandleFindNodeMessage(byte[] message)
    {
        _logger.LogInformation("Received message type => {MessageType}", MessageType.FindNode);
        var decodedMessage = (FindNodeMessage)_messageDecoder.DecodeMessage(message);
        var distances = decodedMessage.Distances.Take(RecordLimit);
        var closestNodes = _routingTable.GetEnrRecordsAtDistances(distances).ToArray();
        var nodesMessage = new NodesMessage(decodedMessage.RequestId, closestNodes.Length, closestNodes);
        
        return nodesMessage.EncodeMessage();
    }
    
    private byte[]? HandleNodesMessage(byte[] message)
    {
        _logger.LogInformation("Received message type => {MessageType}", MessageType.Nodes);
        var decodedMessage = (NodesMessage)_messageDecoder.DecodeMessage(message);

        if(decodedMessage.Enrs.Length == 0)
            return null;

        var pendingRequest = ValidateRequest(decodedMessage);
        
        if(pendingRequest == null)
            return null;
        
        var findNodesRequest = (FindNodeMessage)_messageDecoder.DecodeMessage(pendingRequest.Message.EncodeMessage());
        var identityVerifier = new IdentitySchemeV4Verifier();
        
        foreach (var enr in decodedMessage.Enrs)
        {
            var nodeId = identityVerifier.GetNodeIdFromRecord(enr);

            foreach (var distance in findNodesRequest.Distances)
            {
                var distanceToNode = TableUtility.Log2Distance(nodeId, pendingRequest.NodeId);
                
                if (distance == distanceToNode)
                {
                    _routingTable.UpdateTable(enr);
                    break;
                }
            }
        }

        var nodes = decodedMessage.Enrs.Select(x => new NodeTableEntry(x, new IdentitySchemeV4Verifier())).ToList();
        _lookupManager.ReceiveNodesResponse(nodes);
        _routingTable.MarkNodeAsQueried(pendingRequest.NodeId);

        return null;
    }
    
    private byte[]? HandleTalkReqMessage(byte[] message)
    {
        if (_talkResponder == null)
        {
            _logger.LogWarning("Talk responder is not set. Cannot handle talk request message");
            return null;
        }
        
        _logger.LogInformation("Received message type => {MessageType}", MessageType.TalkReq);
        var decodedMessage = (TalkReqMessage)_messageDecoder.DecodeMessage(message);
        var pendingRequest = ValidateRequest(decodedMessage);
        
        if(pendingRequest == null)
            return null;
        
        var result = _talkResponder.RespondToRequest(decodedMessage.Protocol, decodedMessage.Request);

        if (result)
        {
            _pendingRequests.RemovePendingRequest(decodedMessage.RequestId);
        }
        
        return null;
    }
    
    private byte[]? HandleTalkRespMessage(byte[] message)
    {
        if (_talkResponder == null)
        {
            _logger.LogWarning("Talk responder is not set. Cannot handle talk response message");
            return null;
        }

        _logger.LogInformation("Received message type => {MessageType}", MessageType.TalkResp);
        var decodedMessage = (TalkRespMessage)_messageDecoder.DecodeMessage(message);
        var pendingRequest = ValidateRequest(decodedMessage);
        
        if(pendingRequest == null)
            return null;

        var result  = _talkResponder.HandleResponse(decodedMessage.Response);
        
        if (result)
        {
            _pendingRequests.RemovePendingRequest(decodedMessage.RequestId);
        }
        
        return null;
    }
    
    private PendingRequest? ValidateRequest(Message message)
    {
        var result = _pendingRequests.ContainsPendingRequest(message.RequestId);
        
        if (result == false)
        {
            _logger.LogWarning("Received message with unknown request id. Message Type: {MessageType}, Request id: {RequestId}", message.MessageType, Convert.ToHexString(message.RequestId));
            return null;
        }

        var request = _pendingRequests.GetPendingRequest(message.RequestId);

        if (request != null) 
            return request;
        
        _logger.LogWarning("Pending request is null. Cannot handle message. Message Type: {MessageType}, Request id: {RequestId}", message.MessageType, Convert.ToHexString(message.RequestId));
        return null;
    }
    
}