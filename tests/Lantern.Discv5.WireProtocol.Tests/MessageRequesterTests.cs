using Lantern.Discv5.WireProtocol.Connection;
using Lantern.Discv5.WireProtocol.Identity;
using Lantern.Discv5.WireProtocol.Logging;
using Lantern.Discv5.WireProtocol.Message;
using Lantern.Discv5.WireProtocol.Message.Requests;
using Lantern.Discv5.WireProtocol.Message.Responses;
using Lantern.Discv5.WireProtocol.Session;
using Lantern.Discv5.WireProtocol.Table;
using Lantern.Discv5.WireProtocol.Utility;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Lantern.Discv5.WireProtocol.Tests;

[TestFixture]
public class MessageRequesterTests
{
    private static IIdentityManager _identityManager = null!;
    private static IMessageRequester _messageRequester = null!;

    [SetUp]
    public void Setup()
    {
        var connectionOptions = ConnectionOptions.Default;
        var sessionOptions = SessionOptions.Default;
        var tableOptions = TableOptions.Default;
        var loggerFactory = LoggingOptions.Default; 
        var serviceProvider =
            ServiceConfiguration.ConfigureServices(loggerFactory, connectionOptions, sessionOptions, tableOptions).BuildServiceProvider();
        
        _identityManager = serviceProvider.GetRequiredService<IIdentityManager>();
        _messageRequester = serviceProvider.GetRequiredService<IMessageRequester>();
    }

    [Test]
    public void Test_MessageRequester_ShouldGeneratePingMessageCorrectly()
    {
        var destNodeId = RandomUtility.GenerateRandomData(32);
        var pingMessage = _messageRequester.ConstructPingMessage(destNodeId)!;
        var decodedPingMessage = (PingMessage)new MessageDecoder().DecodeMessage(pingMessage);

        Assert.AreEqual(MessageType.Ping, decodedPingMessage.MessageType);
        Assert.AreEqual(_identityManager.Record.SequenceNumber, decodedPingMessage.EnrSeq);
    }
    
    [Test]
    public void Test_MessageRequester_ShouldGenerateFindNodeMessageCorrectly()
    {
        var destNodeId = RandomUtility.GenerateRandomData(32);
        var targetNodeId = RandomUtility.GenerateRandomData(32);
        var findNodeMessage = _messageRequester.ConstructFindNodeMessage(destNodeId, targetNodeId)!;
        var decodedFindNodeMessage = (FindNodeMessage)new MessageDecoder().DecodeMessage(findNodeMessage);

        Assert.AreEqual(MessageType.FindNode, decodedFindNodeMessage.MessageType);
        Assert.AreEqual(TableUtility.Log2Distance(targetNodeId, destNodeId), decodedFindNodeMessage.Distances.First());
    }
    
    [Test]
    public void Test_MessageRequester_ShouldGenerateTalkRequestMessageCorrectly()
    {
        var destNodeId = RandomUtility.GenerateRandomData(32);
        var protocol = "discv5"u8.ToArray();
        var request = "ping"u8.ToArray();
        var talkRequestMessage = _messageRequester.ConstructTalkReqMessage(destNodeId, protocol, request)!;
        var decodedTalkRequestMessage = (TalkReqMessage)new MessageDecoder().DecodeMessage(talkRequestMessage);

        Assert.AreEqual(MessageType.TalkReq, decodedTalkRequestMessage.MessageType);
        Assert.AreEqual(protocol, decodedTalkRequestMessage.Protocol);
        Assert.AreEqual(request, decodedTalkRequestMessage.Request);
    }

    [Test]
    public void Test_MessageRequester_ShouldGenerateTalkResponseMessageCorrectly()
    {
        var destNodeId = RandomUtility.GenerateRandomData(32);
        var response = "response"u8.ToArray();
        var talkResponseMessage = _messageRequester.ConstructTalkRespMessage(destNodeId, response)!;
        var decodedTalkResponseMessage = (TalkRespMessage)new MessageDecoder().DecodeMessage(talkResponseMessage);

        Assert.AreEqual(MessageType.TalkResp, decodedTalkResponseMessage.MessageType);
        Assert.AreEqual(response, decodedTalkResponseMessage.Response);
    }
}