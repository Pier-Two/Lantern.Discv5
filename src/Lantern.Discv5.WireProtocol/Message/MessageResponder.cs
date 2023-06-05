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
    private readonly IRequestManager _requestManager;
    private readonly ILookupManager _lookupManager;
    private readonly ITalkReqAndRespHandler? _talkResponder;
    private readonly IMessageDecoder _messageDecoder;
    private readonly ILogger<MessageResponder> _logger;

    public MessageResponder(IIdentityManager identityManager, IRoutingTable routingTable, IRequestManager requestManager, ILookupManager lookupManager, IMessageDecoder messageDecoder, ILoggerFactory loggerFactory, ITalkReqAndRespHandler? talkResponder = null)
    {
        _identityManager = identityManager;
        _routingTable = routingTable;
        _requestManager = requestManager;
        _lookupManager = lookupManager;
        _messageDecoder = messageDecoder;
        _talkResponder = talkResponder;
        _logger = loggerFactory.CreateLogger<MessageResponder>();
    }
    
    public async Task<byte[]?> HandleMessage(byte[] message, IPEndPoint endPoint)
    {
        var messageType = (MessageType)message[0];

        return messageType switch
        {
            MessageType.Ping => HandlePingMessage(message, endPoint),
            MessageType.Pong => HandlePongMessage(message),
            MessageType.FindNode => HandleFindNodeMessage(message),
            MessageType.Nodes => await HandleNodesMessage(message),
            MessageType.TalkReq => HandleTalkReqMessage(message),
            MessageType.TalkResp => HandleTalkRespMessage(message),
            _ => throw new ArgumentOutOfRangeException()
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
        var pendingRequest = GetPendingRequest(decodedMessage);

        if (pendingRequest == null)
        {
            return null;
        }

        var nodeEntry = _routingTable.GetNodeEntry(pendingRequest.NodeId);

        if (nodeEntry == null)
        {
            _logger.LogWarning("ENR record is not known. Cannot handle PONG message from node. Node ID: {NodeId}", Convert.ToHexString(pendingRequest.NodeId));
            return null;
        }
        
        var enrRecord = nodeEntry.Record;
        var nodeId = new IdentitySchemeV4Verifier().GetNodeIdFromRecord(enrRecord);

        // This condition will actually need to be removed because bootnodes will be added to the routing table first and then pinged.
        if (nodeEntry.IsLive == false)
        {
            _routingTable.UpdateTable(enrRecord);
            _routingTable.MarkNodeAsLive(nodeId);

            if (!_identityManager.IsIpAddressAndPortSet())
            {
                var endpoint = new IPEndPoint(decodedMessage.RecipientIp, decodedMessage.RecipientPort);
                _identityManager.UpdateIpAddressAndPort(endpoint);
            }

            return null;
        }
        
        _routingTable.MarkNodeAsLive(nodeId);
        
        if (decodedMessage.EnrSeq <= (int)enrRecord.SequenceNumber)
        {
            return null;
        }
        
        var distance = new []{ 0 };
        var findNodesMessage = new FindNodeMessage(distance);
        
        var result = _requestManager.AddPendingRequest(findNodesMessage.RequestId, new PendingRequest(pendingRequest.NodeId, findNodesMessage));

        if(result == false)
        {
            _logger.LogWarning("Failed to add pending request. Request id: {RequestId}", Convert.ToHexString(findNodesMessage.RequestId));
            return null;
        }

        return findNodesMessage.EncodeMessage();
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
    
    private async Task <byte[]?> HandleNodesMessage(byte[] message)
    {
        _logger.LogInformation("Received message type => {MessageType}", MessageType.Nodes);
        var decodedMessage = (NodesMessage)_messageDecoder.DecodeMessage(message);

        if (decodedMessage.Enrs.Length == 0)
        {
            _logger.LogWarning("Received NODES message with no ENRs. Ignoring message");
            return null;
        }

        var pendingRequest = GetPendingRequest(decodedMessage);
    
        if (pendingRequest == null)
        {
            _logger.LogWarning("Received NODES message with no pending request. Ignoring message");
            return null; 
        }
        
        pendingRequest.MaxResponses = decodedMessage.Total;

        if (pendingRequest.ResponsesCount > decodedMessage.Total)
        {
            _logger.LogWarning("Received {ResponsesCount} responses so far. Expected {TotalResponses} from node {NodeId}. Ignoring response", pendingRequest.ResponsesCount, decodedMessage.Total, Convert.ToHexString(pendingRequest.NodeId));
            return null;
        }
        
        var senderNodeEntry = _routingTable.GetNodeEntry(pendingRequest.NodeId);
        var findNodesRequest = (FindNodeMessage)_messageDecoder.DecodeMessage(pendingRequest.Message.EncodeMessage());
        var receivedNodes = new List<NodeTableEntry>();

        foreach (var distance in findNodesRequest.Distances)
        {
            foreach (var enr in decodedMessage.Enrs)
            {
                var nodeId = _identityManager.Verifier.GetNodeIdFromRecord(enr);
                var distanceToNode = TableUtility.Log2Distance(nodeId, pendingRequest.NodeId);

                if (distance == distanceToNode)
                {
                    if (_routingTable.GetNodeEntry(nodeId) == null)
                    {
                        _routingTable.UpdateTable(enr);
                    }
                    
                    var nodeEntry = _routingTable.GetNodeEntry(nodeId);
                    if (nodeEntry != null)
                    {
                        receivedNodes.Add(nodeEntry);
                    }
                }
            }
        }

        if (senderNodeEntry is { IsLive: false })
        {
            _routingTable.MarkNodeAsLive(senderNodeEntry.Id);
        }
        
        _routingTable.MarkNodeAsQueried(pendingRequest.NodeId);
        await _lookupManager.ContinueLookup(receivedNodes, pendingRequest.NodeId, decodedMessage.Total);
        
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
        var pendingRequest = GetPendingRequest(decodedMessage);
        
        if(pendingRequest == null)
            return null;
        
        _talkResponder.HandleRequest(decodedMessage.Protocol, decodedMessage.Request);

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
        var pendingRequest = GetPendingRequest(decodedMessage);
        
        if(pendingRequest == null)
            return null;

        _talkResponder.HandleResponse(decodedMessage.Response);

        return null;
    }
    
    private PendingRequest? GetPendingRequest(Message message)
    {
        var result = _requestManager.ContainsPendingRequest(message.RequestId);
        
        if (result == false)
        {
            _logger.LogWarning("Received message with unknown request id. Message Type: {MessageType}, Request id: {RequestId}", message.MessageType, Convert.ToHexString(message.RequestId));
            return null;
        }

        _requestManager.MarkRequestAsFulfilled(message.RequestId);
        return _requestManager.GetPendingRequest(message.RequestId);
    }
}