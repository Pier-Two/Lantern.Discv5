using System.Net;
using Lantern.Discv5.WireProtocol.Connection;
using Lantern.Discv5.WireProtocol.Identity;
using Lantern.Discv5.WireProtocol.Logging;
using Lantern.Discv5.WireProtocol.Message;
using Lantern.Discv5.WireProtocol.Message.Requests;
using Lantern.Discv5.WireProtocol.Message.Responses;
using Lantern.Discv5.WireProtocol.Session;
using Lantern.Discv5.WireProtocol.Table;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Lantern.Discv5.WireProtocol.Tests;

[TestFixture]
public class MessageResponderTests
{
    private static IMessageResponder _messageResponder = null!;
    private static IIdentityManager _identityManager = null!;

    [SetUp]
    public void Setup()
    {
        var connectionOptions = new ConnectionOptions.Builder()
            .WithPort(Random.Shared.Next(0, 9000))
            .Build();
        var sessionOptions = SessionOptions.Default;
        var tableOptions = TableOptions.Default;
        var loggerFactory = LoggingOptions.Default;
        var serviceProvider = ServiceConfiguration.ConfigureServices(loggerFactory, connectionOptions, sessionOptions, tableOptions, new TestTalkReqAndRespHandler()).BuildServiceProvider();
        
        _messageResponder = serviceProvider.GetRequiredService<IMessageResponder>();
        _identityManager = serviceProvider.GetRequiredService<IIdentityManager>();
    }
    
    [Test]
    public void Test_MessageResponder_ShouldThrowArgumentException_WhenMessageIsNotSupported()
    {
        var topicMessage = new TopicQueryMessage("test"u8.ToArray());
        Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () => await _messageResponder.HandleMessage(topicMessage.EncodeMessage(), new IPEndPoint(IPAddress.Any, 9000)));
    }

    [Test]
    public async Task Test_MessageResponder_ShouldHandlePingMessageCorrectly()
    {
        var pingMessage = new PingMessage((int)_identityManager.Record.SequenceNumber);
        var ipEndPoint = new IPEndPoint(IPAddress.Any, 9989);
        var response = await _messageResponder.HandleMessage(pingMessage.EncodeMessage(), ipEndPoint);
        var pongMessage = (PongMessage)new MessageDecoder().DecodeMessage(response!);
        
        Assert.NotNull(pongMessage);
        Assert.AreEqual(MessageType.Pong, pongMessage.MessageType);
        Assert.AreEqual(_identityManager.Record.SequenceNumber, pongMessage.EnrSeq);
        Assert.AreEqual(ipEndPoint.Address, pongMessage.RecipientIp);
        Assert.AreEqual(ipEndPoint.Port, pongMessage.RecipientPort);
    }

    [Test]
    public async Task Test_MessageResponder_ShouldHandleFindNodesMessageCorrectly()
    {
        var distances = new [] { 252, 253, 254 };
        var findNodesMessage = new FindNodeMessage(distances);
        var ipEndPoint = new IPEndPoint(IPAddress.Any, 9312);
        var response = await _messageResponder.HandleMessage(findNodesMessage.EncodeMessage(), ipEndPoint);
        var nodesMessage = (NodesMessage)new MessageDecoder().DecodeMessage(response!);
        Assert.NotNull(nodesMessage);
        Assert.AreEqual(MessageType.Nodes, nodesMessage.MessageType);
        Assert.AreEqual(0, nodesMessage.Total);
        Assert.AreEqual(0, nodesMessage.Enrs.Length);
    }
    
    [Test]
    public async Task Test_MessageResponder_ShouldHandleTalkRequestMessageCorrectly()
    {
        var talkRequestMessage = new TalkReqMessage("protocol"u8.ToArray(), "request"u8.ToArray());
        var ipEndPoint = new IPEndPoint(IPAddress.Any, 9312);
        var response = await _messageResponder.HandleMessage(talkRequestMessage.EncodeMessage(), ipEndPoint);
        var talkRespMessage = (TalkRespMessage)new MessageDecoder().DecodeMessage(response!);
        Assert.NotNull(talkRespMessage);
        Assert.AreEqual(MessageType.TalkResp, talkRespMessage.MessageType);
        Assert.AreEqual("request"u8.ToArray(), talkRespMessage.Response);
    }

    private class TestTalkReqAndRespHandler : ITalkReqAndRespHandler
    {
        public byte[]? HandleRequest(byte[] protocol, byte[] request)
        {
            return request;
        }

        public byte[]? HandleResponse(byte[] response)
        {
            return response;
        }
    }
}