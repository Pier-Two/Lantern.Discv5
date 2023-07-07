using System.Net;
using System.Net.Sockets;
using Lantern.Discv5.WireProtocol.Connection;
using Lantern.Discv5.WireProtocol.Message;
using Lantern.Discv5.WireProtocol.Packet;
using Lantern.Discv5.WireProtocol.Packet.Handlers;
using Lantern.Discv5.WireProtocol.Packet.Headers;
using Lantern.Discv5.WireProtocol.Session;
using Lantern.Discv5.WireProtocol.Table;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace Lantern.Discv5.WireProtocol.Tests;

[TestFixture]
public class OrdinaryPacketHandlerTests
{
    private Mock<IPacketProcessor> mockPacketProcessor;
    private Mock<ISessionManager> mockSessionManager;
    private Mock<IRoutingTable> mockRoutingTable;
    private Mock<IMessageResponder> mockMessageResponder;
    private Mock<IRequestManager> mockRequestManager;
    private Mock<IUdpConnection> mockUdpConnection;
    private Mock<IPacketBuilder> mockPacketBuilder;
    private Mock<ILoggerFactory> mockLoggerFactory;
    private Mock<ILogger<OrdinaryPacketHandler>> logger;
    
    private Dictionary<int, Times> timesLookup = new()
    {
        { 1, Times.Once() },
        { 2, Times.AtLeastOnce() }
    };

    [SetUp]
    public void Init()
    {
        mockPacketProcessor = new Mock<IPacketProcessor>();
        mockSessionManager = new Mock<ISessionManager>();
        mockRoutingTable = new Mock<IRoutingTable>();
        mockMessageResponder = new Mock<IMessageResponder>();
        mockRequestManager = new Mock<IRequestManager>();
        mockUdpConnection = new Mock<IUdpConnection>();
        mockPacketBuilder = new Mock<IPacketBuilder>();
        logger = new Mock<ILogger<OrdinaryPacketHandler>>();
        mockLoggerFactory = new Mock<ILoggerFactory>();
    }

    [Test]
    [TestCase("18.223.219.100", 9000, LogLevel.Warning, 1)]
    [TestCase("18.223.219.101", 9001, LogLevel.Error, 2)]
    public async Task Test_HandlePacket_NodeEntryIsNull(string ip, int port, LogLevel logLevel, int timesKey)
    {
        mockPacketProcessor.Setup(x => x.GetStaticHeader(new byte[32])).Returns(new StaticHeader("test", new byte[32], new byte[32], 0, new byte[32]));
        mockSessionManager.Setup(x => x.GetSession(It.IsAny<byte[]>(), It.IsAny<IPEndPoint>())).Returns((SessionMain?)null);
        mockUdpConnection.Setup(x => x.SendAsync(It.IsAny<byte[]>(), It.IsAny<IPEndPoint>())).Returns(Task.CompletedTask);
        logger.Setup(x => x.Log(It.IsAny<LogLevel>(), It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(), (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()));
        mockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(logger.Object); 
        
        var handler = new OrdinaryPacketHandler(mockSessionManager.Object, mockRoutingTable.Object,
            mockMessageResponder.Object,  mockRequestManager.Object, mockUdpConnection.Object, mockPacketBuilder.Object, mockPacketProcessor.Object,
            mockLoggerFactory.Object);
        var fakeResult = new UdpReceiveResult(new byte[32], new IPEndPoint(IPAddress.Parse(ip), port));
        await handler.HandlePacket(fakeResult);
        
        logger.Verify(
            x => x.Log(
                logLevel,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()
            ), timesLookup[timesKey]); 
    }

    [Test]
    [TestCase("18.223.219.100", 9000, LogLevel.Warning, 1)]
    [TestCase("18.223.219.101", 9001, LogLevel.Error, 2)]
    public async Task Test_HandlePacket_SessionIsNull(string ip, int port, LogLevel logLevel, int timesKey)
    {
        mockPacketProcessor.Setup(x => x.GetStaticHeader(new byte[32])).Returns(new StaticHeader("test", new byte[32], new byte[32], 0, new byte[32]));
        mockSessionManager.Setup(x => x.GetSession(It.IsAny<byte[]>(), It.IsAny<IPEndPoint>())).Returns((SessionMain?)null);
        mockUdpConnection.Setup(x => x.SendAsync(It.IsAny<byte[]>(), It.IsAny<IPEndPoint>())).Returns(Task.CompletedTask);
        logger.Setup(x => x.Log(It.IsAny<LogLevel>(), It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(), (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()));
        mockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(logger.Object); 

    }
}