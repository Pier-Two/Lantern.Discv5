using System.Net;
using Lantern.Discv5.Enr.EnrContent;
using Lantern.Discv5.Enr.EnrContent.Entries;
using Lantern.Discv5.Enr.IdentityScheme.V4;
using Lantern.Discv5.WireProtocol.Identity;
using Lantern.Discv5.WireProtocol.Message.Decoders;
using Lantern.Discv5.WireProtocol.Message.Requests;
using Lantern.Discv5.WireProtocol.Message.Responses;
using Lantern.Discv5.WireProtocol.Table;

namespace Lantern.Discv5.WireProtocol.Message;

public class MessageResponder : IMessageResponder
{
    private const int RecordLimit = 16;
    private readonly IIdentityManager _identityManager;
    private readonly ITableManager _tableManager;
    private readonly IPendingRequests _pendingRequests;
    private readonly ILookupManager _lookupManager;
    private readonly ITalkResponder? _talkResponder;

    public MessageResponder(IIdentityManager identityManager, ITableManager tableManager, IPendingRequests pendingRequests, ILookupManager lookupManager, ITalkResponder? talkResponder = null)
    {
        _identityManager = identityManager;
        _tableManager = tableManager;
        _pendingRequests = pendingRequests;
        _lookupManager = lookupManager;
        _talkResponder = talkResponder;
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
        Console.Write("Received message type => " + MessageType.Ping);
        var decodedMessage = new PingDecoder().DecodeMessage(message);
        var localEnrSeq = _identityManager.Record.SequenceNumber;
        var pongMessage = new PongMessage(decodedMessage.RequestId, (int)localEnrSeq, endPoint.Address, endPoint.Port);
        return pongMessage.EncodeMessage();
    }
    
    private byte[]? HandlePongMessage(byte[] message)
    {
        Console.Write("Received message type => " + MessageType.Pong + "\n");
        var decodedMessage = new PongDecoder().DecodeMessage(message);
        var result = _pendingRequests.ContainsPendingRequest(decodedMessage.RequestId);
        
        if (result == false)
        {
            Console.WriteLine(" => Received pong message with unknown request id. Request id: " + Convert.ToHexString(decodedMessage.RequestId));
            return null;
        }

        var pendingRequest = ValidateRequest(decodedMessage);
        
        if(pendingRequest == null)
            return null;
        
        var nodeEntry = _tableManager.GetNodeEntry(pendingRequest.NodeId);

        if (nodeEntry == null)
        {
            Console.WriteLine("ENR record is not known.");
            return null;
        }
        
        var enrRecord = nodeEntry.Record;
        
        if (nodeEntry.IsLive == false)
        {
            _tableManager.UpdateTable(enrRecord);
            _pendingRequests.RemovePendingRequest(decodedMessage.RequestId);

            Console.WriteLine("\nAdded bootstrap enr record to table. ENR: " + Convert.ToHexString(new IdentitySchemeV4Verifier().GetNodeIdFromRecord(enrRecord)));

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
        Console.Write("Received message type => " + MessageType.FindNode);
        var decodedMessage = new FindNodeDecoder().DecodeMessage(message);
        var distances = decodedMessage.Distances.Take(RecordLimit);
        var closestNodes = _tableManager.GetEnrRecordsAtDistances(distances).ToArray();
        var nodesMessage = new NodesMessage(decodedMessage.RequestId, closestNodes.Length, closestNodes);
        return nodesMessage.EncodeMessage();
    }
    
    private byte[]? HandleNodesMessage(byte[] message)
    {
        Console.Write("Received message type => " + MessageType.Nodes);
        var decodedMessage = new NodesDecoder().DecodeMessage(message);

        if(decodedMessage.Enrs.Length == 0)
            return null;

        var pendingRequest = ValidateRequest(decodedMessage);
        
        if(pendingRequest == null)
            return null;
        
        var findNodesRequest = new FindNodeDecoder().DecodeMessage(pendingRequest.Message.EncodeMessage());
        var identityVerifier = new IdentitySchemeV4Verifier();

        Console.WriteLine();
        foreach (var enr in decodedMessage.Enrs)
        {
            var nodeId = identityVerifier.GetNodeIdFromRecord(enr);

            foreach (var distance in findNodesRequest.Distances)
            {
                var distanceToNode = TableUtility.Log2Distance(nodeId, pendingRequest.NodeId);
                
                if (distance == distanceToNode)
                {
                    _tableManager.UpdateTable(enr);
                    Console.WriteLine("Added enr record to table. Node ID: " + Convert.ToHexString(identityVerifier.GetNodeIdFromRecord(enr)));
                    break;
                }
            }
        }

        var nodes = decodedMessage.Enrs.Select(x => new NodeTableEntry(x, new IdentitySchemeV4Verifier())).ToList();
        _lookupManager.ReceiveNodesResponse(nodes);
        _tableManager.MarkNodeAsQueried(pendingRequest.NodeId);

        return null;
    }
    
    private byte[]? HandleTalkReqMessage(byte[] message)
    {
        if (_talkResponder == null)
        {
            Console.WriteLine("Talk responder is not set.");
            return null;
        }
        
        Console.Write("Received message type => " + MessageType.TalkReq);
        var decodedMessage = new TalkReqDecoder().DecodeMessage(message);
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
            Console.WriteLine("Talk responder is not set.");
            return null;
        }

        Console.Write("Received message type => " + MessageType.TalkResp);
        var decodedMessage = new TalkRespDecoder().DecodeMessage(message);
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
            Console.WriteLine(" => Received message with unknown request id. Message Type: " + message.MessageType + " Request id: " + Convert.ToHexString(message.RequestId));
            return null;
        }

        var request = _pendingRequests.GetPendingRequest(message.RequestId);

        if (request != null) 
            return request;
        
        Console.WriteLine("Pending request is null.");
        return null;
    }
    
}