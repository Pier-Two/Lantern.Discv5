using System.Net;
using Lantern.Discv5.Enr;
using Lantern.Discv5.WireProtocol.Identity;
using Lantern.Discv5.WireProtocol.Message.Requests;
using Lantern.Discv5.WireProtocol.Message.Responses;
using Lantern.Discv5.WireProtocol.Table;
using Microsoft.Extensions.Logging;

namespace Lantern.Discv5.WireProtocol.Message;

public class MessageResponder : IMessageResponder
{
    private const int RecordLimit = 16;
    private const int MaxRecordsPerMessage = 3;
    private readonly IIdentityManager _identityManager;
    private readonly IRoutingTable _routingTable;
    private readonly IRequestManager _requestManager;
    private readonly ILookupManager _lookupManager;
    private readonly ITalkReqAndRespHandler? _talkResponder;
    private readonly IMessageDecoder _messageDecoder;
    private readonly ILogger<MessageResponder> _logger;

    public MessageResponder(
        IIdentityManager identityManager, 
        IRoutingTable routingTable, 
        IRequestManager requestManager, 
        ILookupManager lookupManager, 
        IMessageDecoder messageDecoder, 
        ILoggerFactory loggerFactory, 
        ITalkReqAndRespHandler? talkResponder = null)
    {
        _identityManager = identityManager;
        _routingTable = routingTable;
        _requestManager = requestManager;
        _lookupManager = lookupManager;
        _messageDecoder = messageDecoder;
        _talkResponder = talkResponder;
        _logger = loggerFactory.CreateLogger<MessageResponder>();
    }
    
    public async Task<byte[][]?> HandleMessageAsync(byte[] message, IPEndPoint endPoint)
    {
        var messageType = (MessageType)message[0];

        return messageType switch
        {
            MessageType.Ping => HandlePingMessage(message, endPoint),
            MessageType.Pong => HandlePongMessage(message),
            MessageType.FindNode => HandleFindNodeMessage(message),
            MessageType.Nodes => await HandleNodesMessageAsync(message),
            MessageType.TalkReq => HandleTalkReqMessage(message),
            MessageType.TalkResp => HandleTalkRespMessage(message),
            _ => null
        };
    }

    private byte[][] HandlePingMessage(byte[] message, IPEndPoint endPoint)
    {
        _logger.LogInformation("Received message type => {MessageType}", MessageType.Ping);
        var decodedMessage = _messageDecoder.DecodeMessage(message);
        var localEnrSeq = _identityManager.Record.SequenceNumber;
        var pongMessage = new PongMessage(decodedMessage.RequestId, (int)localEnrSeq, endPoint.Address, endPoint.Port);
        var responseMessage = new List<byte[]> { pongMessage.EncodeMessage() };

        return responseMessage.ToArray();
    }
    
    private byte[][]? HandlePongMessage(byte[] message)
    {
        _logger.LogInformation("Received message type => {MessageType}", MessageType.Pong);
        
        var decodedMessage = (PongMessage)_messageDecoder.DecodeMessage(message);
        var pendingRequest = GetPendingRequest(decodedMessage);

        if (pendingRequest == null)
        {
            _logger.LogWarning("Received PONG message with no pending request. Ignoring message");
            return null;
        }

        var nodeEntry = _routingTable.GetNodeEntry(pendingRequest.NodeId);

        if (nodeEntry == null)
        {
            _logger.LogWarning("ENR record is not known. Cannot handle PONG message from node. Node ID: {NodeId}", Convert.ToHexString(pendingRequest.NodeId));
            return null;
        }
        
        if (nodeEntry.Status != NodeStatus.Live)
        {
            _routingTable.UpdateFromEnr(nodeEntry.Record);
            _routingTable.MarkNodeAsLive(nodeEntry.Id);
            _routingTable.MarkNodeAsResponded(pendingRequest.NodeId);

            if (!_identityManager.IsIpAddressAndPortSet())
            {
                var endpoint = new IPEndPoint(decodedMessage.RecipientIp, decodedMessage.RecipientPort);
                _identityManager.UpdateIpAddressAndPort(endpoint);
            }

            return null;
        }

        if (decodedMessage.EnrSeq <= (int)nodeEntry.Record.SequenceNumber)
        {
            return null;
        }
        
        var distance = new []{ 0 };
        var findNodesMessage = new FindNodeMessage(distance);
        var result = _requestManager.AddPendingRequest(findNodesMessage.RequestId, new PendingRequest(pendingRequest.NodeId, findNodesMessage));

        if(!result)
        {
            _logger.LogWarning("Failed to add pending request. Request id: {RequestId}", Convert.ToHexString(findNodesMessage.RequestId));
            return null;
        }

        var responseMessage = new List<byte[]> { findNodesMessage.EncodeMessage() };

        return responseMessage.ToArray();
    }
    
    private byte[][] HandleFindNodeMessage(byte[] message)
    {
        _logger.LogInformation("Received message type => {MessageType}", MessageType.FindNode);
        var decodedMessage = (FindNodeMessage)_messageDecoder.DecodeMessage(message);
        var closestNodes = _routingTable.GetEnrRecordsAtDistances(decodedMessage.Distances).Take(RecordLimit).ToArray();
        var chunkedRecords = SplitIntoChunks(closestNodes, MaxRecordsPerMessage);
        var responses = chunkedRecords.Select(chunk => new NodesMessage(decodedMessage.RequestId, chunk.Length, chunk)).Select(nodesMessage => nodesMessage.EncodeMessage()).ToArray();
        
        if(responses.Length == 0)
        {
            var response = new NodesMessage(decodedMessage.RequestId, closestNodes.Length, Array.Empty<EnrRecord>())
                .EncodeMessage();
            return new List<byte[]> { response }.ToArray();
        }
        
        _logger.LogInformation("Sending a total of {EnrRecords} with {Responses} responses", closestNodes.Length, responses.Length);
        
        return responses;
    }

    private async Task <byte[][]?> HandleNodesMessageAsync(byte[] message)
    {
        _logger.LogInformation("Received message type => {MessageType}", MessageType.Nodes);
        var decodedMessage = (NodesMessage)_messageDecoder.DecodeMessage(message);
        var pendingRequest = GetPendingRequest(decodedMessage);
    
        if (pendingRequest == null)
        {
            _logger.LogWarning("Received NODES message with no pending request. Ignoring message");
            return null; 
        }
        
        pendingRequest.MaxResponses = decodedMessage.Total;

        if (pendingRequest.ResponsesCount > decodedMessage.Total)
        {
            _logger.LogWarning("Expected {ExpectedResponsesCount} from node {NodeId} but received {TotalResponsesCount}. Ignoring response", decodedMessage.Total, Convert.ToHexString(pendingRequest.NodeId), pendingRequest.ResponsesCount);
            return null;
        }
        
        var findNodesRequest = (FindNodeMessage)_messageDecoder.DecodeMessage(pendingRequest.Message.EncodeMessage());
        var receivedNodes = new List<NodeTableEntry>();

        try
        {
            foreach (var distance in findNodesRequest.Distances)
            {
                foreach (var enr in decodedMessage.Enrs)
                {
                    var nodeId = _identityManager.Verifier.GetNodeIdFromRecord(enr);
                    var distanceToNode = TableUtility.Log2Distance(nodeId, pendingRequest.NodeId);

                    if (distance != distanceToNode) 
                        continue;
                
                    if (_routingTable.GetNodeEntry(nodeId) == null)
                    {
                        _routingTable.UpdateFromEnr(enr);
                    }
                    
                    var nodeEntry = _routingTable.GetNodeEntry(nodeId);
                    if (nodeEntry != null)
                    {
                        receivedNodes.Add(nodeEntry);
                    }
                }
            }  
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling NODES message");
            return null;
        }
        
        await _lookupManager.ContinueLookupAsync(receivedNodes, pendingRequest.NodeId, decodedMessage.Total);
        
        return null;
    }
    
    private byte[][]? HandleTalkReqMessage(byte[] message)
    {
        if (_talkResponder == null)
        {
            _logger.LogWarning("Talk responder is not set. Cannot handle talk request message");
            return null;
        }
        
        _logger.LogInformation("Received message type => {MessageType}", MessageType.TalkReq);
        var decodedMessage = (TalkReqMessage)_messageDecoder.DecodeMessage(message);
        var responses = _talkResponder.HandleRequest(decodedMessage.Protocol, decodedMessage.Request);

        if (responses == null)
        {
            return null;
        }
        
        var responseMessages = new List<byte[]>();

        foreach (var response in responses)
        {
            var talkRespMessage = new TalkRespMessage(decodedMessage.RequestId, response);
            responseMessages.Add(talkRespMessage.EncodeMessage());
        }
        
        return responseMessages.ToArray();
    }
    
    private byte[][]? HandleTalkRespMessage(byte[] message)
    {
        if (_talkResponder == null)
        {
            _logger.LogWarning("Talk responder is not set. Cannot handle talk response message");
            return null;
        }

        _logger.LogInformation("Received message type => {MessageType}", MessageType.TalkResp);
        
        var decodedMessage = (TalkRespMessage)_messageDecoder.DecodeMessage(message);
        var pendingRequest = GetPendingRequest(decodedMessage);
        
        if (pendingRequest == null)
        {
            _logger.LogWarning("Received TALKRESP message with no pending request. Ignoring message");
            return null; 
        }
        
        _talkResponder.HandleResponse(decodedMessage.Response);

        return null;
    }
    
    private PendingRequest? GetPendingRequest(Message message)
    {
        var pendingRequest = _requestManager.MarkRequestAsFulfilled(message.RequestId);

        if(pendingRequest == null )
        {
            _logger.LogWarning("Received message with unknown request id. Message Type: {MessageType}, Request id: {RequestId}", message.MessageType, Convert.ToHexString(message.RequestId));
            return null;
        }
        
        _routingTable.MarkNodeAsLive(pendingRequest.NodeId);
        _routingTable.MarkNodeAsResponded(pendingRequest.NodeId);

        return _requestManager.GetPendingRequest(message.RequestId);
    }
    
    private static IEnumerable<T[]> SplitIntoChunks<T>(IReadOnlyCollection<T> array, int chunkSize)
    {
        for (var i = 0; i < array.Count; i += chunkSize)
        {
            yield return array.Skip(i).Take(chunkSize).ToArray();
        }
    }
}