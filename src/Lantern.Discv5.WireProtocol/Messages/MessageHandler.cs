using Lantern.Discv5.WireProtocol.Messages.Decoders;
using Lantern.Discv5.WireProtocol.Table;

namespace Lantern.Discv5.WireProtocol.Messages;

public class MessageHandler : IMessageHandler
{
    private const int RecordLimit = 16;
    private readonly ITableManager _tableManager;

    public MessageHandler(ITableManager tableManager)
    {
        _tableManager = tableManager;
    }
    public void HandleMessage(byte[] message)
    {
        var messageType = (MessageType)message[0];

        switch (messageType)
        {
            case MessageType.Ping: 
                Console.Write("Received message type: " + MessageType.Ping);
                HandlePingMessage(message);
                break;
            case MessageType.Pong:
                Console.Write("Received message type: " + MessageType.Pong);
                HandlePongMessage(message);
                break;
            case MessageType.FindNode:
                Console.Write("Received message type: " + MessageType.FindNode);
                HandleFindNodeMessage(message);
                break;
            case MessageType.Nodes:
                Console.Write("Received message type: " + MessageType.Nodes);
                HandleNodesMessage(message);
                break;
            case MessageType.TalkReq:
                Console.Write("Received message type: " + MessageType.TalkReq);
                HandleTalkReqMessage(message);
                break;
            case MessageType.TalkResp:
                Console.Write("Received message type: " + MessageType.TalkResp);
                HandleTalkRespMessage(message);
                break;
            case MessageType.RegTopic:
                Console.Write("Received message type: " + MessageType.RegTopic);
                HandleRegTopicMessage(message);
                break;
            case MessageType.Ticket:
                Console.Write("Received message type: " + MessageType.Ticket);
                HandleTicketMessage(message);
                break;
            case MessageType.RegConfirmation:
                Console.Write("Received message type: " + MessageType.RegConfirmation);
                HandleRegConfirmationMessage(message);
                break;
            case MessageType.TopicQuery:
                Console.Write("Received message type: " + MessageType.TopicQuery);
                HandleTopicQueryMessage(message);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(messageType), messageType, null);
        }
    }
    
    private void HandlePingMessage(byte[] message)
    {
        var decodedMessage = new PingDecoder().DecodeMessage(message);
    }
    
    private void HandlePongMessage(byte[] message)
    {
        var decodedMessage = new PongDecoder().DecodeMessage(message);
    }
    
    private void HandleFindNodeMessage(byte[] message)
    {
        var decodedMessage = new FindNodeDecoder().DecodeMessage(message);
        var distances = decodedMessage.Distances.Take(RecordLimit);

        foreach (var distance in distances)
        {
            
        }
    }
    
    private void HandleNodesMessage(byte[] message)
    {
        var decodedMessage = new NodesDecoder().DecodeMessage(message);
    }
    
    private void HandleTalkReqMessage(byte[] message)
    {
        var decodedMessage = new TalkReqDecoder().DecodeMessage(message);
    }
    
    private void HandleTalkRespMessage(byte[] message)
    {
        var decodedMessage = new TalkRespDecoder().DecodeMessage(message);
    }
    
    private void HandleRegTopicMessage(byte[] message)
    {
        var decodedMessage = new RegTopicDecoder().DecodeMessage(message);
    }
    
    private void HandleTicketMessage(byte[] message)
    {
        var decodedMessage = new TicketDecoder().DecodeMessage(message);
    }
    
    private void HandleRegConfirmationMessage(byte[] message)
    {
        var decodedMessage = new RegConfirmationDecoder().DecodeMessage(message);
    }
    
    private void HandleTopicQueryMessage(byte[] message)
    {
        var decodedMessage = new TopicQueryDecoder().DecodeMessage(message);
    }
}