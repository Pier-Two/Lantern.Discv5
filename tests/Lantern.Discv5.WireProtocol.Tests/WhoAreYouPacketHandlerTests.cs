using System.Net;
using System.Net.Sockets;
using Lantern.Discv5.Enr;
using Lantern.Discv5.Enr.EnrFactory;
using Lantern.Discv5.Enr.IdentityScheme.V4;
using Lantern.Discv5.WireProtocol.Connection;
using Lantern.Discv5.WireProtocol.Identity;
using Lantern.Discv5.WireProtocol.Message;
using Lantern.Discv5.WireProtocol.Message.Requests;
using Lantern.Discv5.WireProtocol.Packet;
using Lantern.Discv5.WireProtocol.Packet.Handlers;
using Lantern.Discv5.WireProtocol.Packet.Headers;
using Lantern.Discv5.WireProtocol.Packet.Types;
using Lantern.Discv5.WireProtocol.Session;
using Lantern.Discv5.WireProtocol.Table;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace Lantern.Discv5.WireProtocol.Tests;

[TestFixture]
public class WhoAreYouPacketHandlerTests
{
    private Mock<IIdentityManager> mockIdentityManager;
    private Mock<ISessionManager> mockSessionManager;
    private Mock<IRequestManager> mockRequestManager;
    private Mock<IRoutingTable> mockRoutingTable;
    private Mock<IUdpConnection> mockUdpConnection;
    private Mock<IPacketBuilder> mockPacketBuilder;
    private Mock<ISessionMain> mockSessionMain;
    private Mock<ILoggerFactory> mockLoggerFactory;
    private Mock<ILogger<HandshakePacketHandler>> logger;
    private Mock<IPacketProcessor> mockPacketProcessor;

    [SetUp]
    public void Init()
    {
        mockIdentityManager = new Mock<IIdentityManager>();
        mockSessionManager = new Mock<ISessionManager>();
        mockRoutingTable = new Mock<IRoutingTable>();
        mockRequestManager = new Mock<IRequestManager>();
        mockUdpConnection = new Mock<IUdpConnection>();
        mockPacketBuilder = new Mock<IPacketBuilder>();
        mockSessionMain = new Mock<ISessionMain>();
        logger = new Mock<ILogger<HandshakePacketHandler>>();
        mockPacketProcessor = new Mock<IPacketProcessor>();
        mockLoggerFactory = new Mock<ILoggerFactory>();
        logger
            .Setup(x => x.Log(It.IsAny<LogLevel>(), It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(), (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()));
        mockLoggerFactory
            .Setup(x => x.CreateLogger(It.IsAny<string>()))
            .Returns(logger.Object);
    }

    [Test]
    public void Test_PacketHandlerType_ShouldReturnWhoAreYouType()
    {
        Assert.AreEqual(PacketType.WhoAreYou, new WhoAreYouPacketHandler(mockIdentityManager.Object, mockSessionManager.Object, mockRoutingTable.Object,
            mockRequestManager.Object, mockUdpConnection.Object, mockPacketBuilder.Object, mockPacketProcessor.Object,mockLoggerFactory.Object).PacketType);
    }
    
    [Test]
    public async Task Test_HandlePacket_ShouldReturn_WhenDestNodeIdIsNull()
    {
        // Test data
        var staticHeader = new StaticHeader("test", new byte[32], new byte[32], 0, new byte[32]);
        
        // Arrange
        mockRequestManager
            .Setup(x => x.GetCachedHandshakeInteraction(It.IsAny<byte[]>()))
            .Returns((byte[]?)null);
        mockPacketProcessor
            .Setup(x => x.GetStaticHeader(It.IsAny<byte[]>()))
            .Returns(staticHeader);
        
        var handler = new WhoAreYouPacketHandler(mockIdentityManager.Object,mockSessionManager.Object, mockRoutingTable.Object,
            mockRequestManager.Object, mockUdpConnection.Object, mockPacketBuilder.Object, 
            mockPacketProcessor.Object, mockLoggerFactory.Object);
        var fakeResult = new UdpReceiveResult(new byte[32], new IPEndPoint(IPAddress.Parse("18.223.219.100"), 9000));

        // Act
        await handler.HandlePacket(fakeResult);
        
        mockRequestManager
            .Verify(x => x.GetCachedHandshakeInteraction(It.IsAny<byte[]>()), Times.Once);
        mockRoutingTable
            .Verify(x => x.GetNodeEntry(It.IsAny<byte[]>()), Times.Never);
    }
    
    [Test]
    public async Task Test_HandlePacket_ShouldReturn_WhenNodeEntryIsNull()
    {
        // Test data
        var staticHeader = new StaticHeader("test", new byte[32], new byte[32], 0, new byte[32]);
        
        // Arrange
        mockRequestManager
            .Setup(x => x.GetCachedHandshakeInteraction(It.IsAny<byte[]>()))
            .Returns(new byte[32]);
        mockRoutingTable
            .Setup(x => x.GetNodeEntry(It.IsAny<byte[]>()))
            .Returns((NodeTableEntry?)null);
        mockPacketProcessor
            .Setup(x => x.GetStaticHeader(It.IsAny<byte[]>()))
            .Returns(staticHeader);
        
        var handler = new WhoAreYouPacketHandler(mockIdentityManager.Object,mockSessionManager.Object, mockRoutingTable.Object,
            mockRequestManager.Object, mockUdpConnection.Object, mockPacketBuilder.Object, 
            mockPacketProcessor.Object, mockLoggerFactory.Object);
        var fakeResult = new UdpReceiveResult(new byte[32], new IPEndPoint(IPAddress.Parse("18.223.219.100"), 9000));

        // Act
        await handler.HandlePacket(fakeResult);
        
        mockRequestManager
            .Verify(x => x.GetCachedHandshakeInteraction(It.IsAny<byte[]>()), Times.Once);
        mockRoutingTable
            .Verify(x => x.GetNodeEntry(It.IsAny<byte[]>()), Times.Once);
        mockSessionManager
            .Verify(x=> x.GetSession(It.IsAny<byte[]>(), It.IsAny<IPEndPoint>()), Times.Never);
    }
    
    [Test]
    public async Task Test_HandlePacket_ShouldReturn_WhenSessionCannotBeCreated()
    {
        // Test data
        var enrRecord = new EnrRecordFactory().CreateFromString("enr:-IS4QHCYrYZbAKWCBRlAy5zzaDZXJBGkcnh4MHcBFZntXNFrdvJjX04jRzjzCBOonrkTfj499SZuOh8R33Ls8RRcy5wBgmlkgnY0gmlwhH8AAAGJc2VjcDI1NmsxoQPKY0yuDUmstAHYpMa2_oxVtw0RW_QAdpzBQA8yWM0xOIN1ZHCCdl8");
        var staticHeader = new StaticHeader("test", new byte[32], new byte[32], 0, new byte[32]);
        
        // Arrange
        mockRequestManager
            .Setup(x => x.GetCachedHandshakeInteraction(It.IsAny<byte[]>()))
            .Returns(new byte[32]);
        mockRoutingTable
            .Setup(x => x.GetNodeEntry(It.IsAny<byte[]>()))
            .Returns(new NodeTableEntry(enrRecord,new IdentitySchemeV4Verifier()));
        mockSessionManager
            .Setup(x => x.GetSession(It.IsAny<byte[]>(), It.IsAny<IPEndPoint>()))
            .Returns((ISessionMain?)null);
        mockSessionManager
            .Setup(x => x.CreateSession(It.IsAny<SessionType>(), It.IsAny<byte[]>(), It.IsAny<IPEndPoint>()))
            .Returns((ISessionMain?)null);
        mockPacketProcessor
            .Setup(x => x.GetStaticHeader(It.IsAny<byte[]>()))
            .Returns(staticHeader);
        
        var handler = new WhoAreYouPacketHandler(mockIdentityManager.Object,mockSessionManager.Object, mockRoutingTable.Object,
            mockRequestManager.Object, mockUdpConnection.Object, mockPacketBuilder.Object, 
            mockPacketProcessor.Object, mockLoggerFactory.Object);
        var fakeResult = new UdpReceiveResult(new byte[32], new IPEndPoint(IPAddress.Parse("18.223.219.100"), 9000));

        // Act
        await handler.HandlePacket(fakeResult);
        
        mockRequestManager
            .Verify(x => x.GetCachedHandshakeInteraction(It.IsAny<byte[]>()), Times.Once);
        mockRoutingTable
            .Verify(x => x.GetNodeEntry(It.IsAny<byte[]>()), Times.Once);
        mockSessionManager
            .Verify(x=> x.GetSession(It.IsAny<byte[]>(), It.IsAny<IPEndPoint>()), Times.Once);
        mockSessionManager
            .Verify(x => x.CreateSession(It.IsAny<SessionType>(), It.IsAny<byte[]>(), It.IsAny<IPEndPoint>()), Times.Once);
        mockRequestManager
            .Verify(x => x.GetCachedRequest(It.IsAny<byte[]>()), Times.Never);
    }
    
    [Test]
    public async Task Test_HandlePacket_ShouldSendRandomPacket_WhenNoReplyMessageIsCreated()
    {
        // Test data
        var enrRecord = new EnrRecordFactory().CreateFromString("enr:-IS4QHCYrYZbAKWCBRlAy5zzaDZXJBGkcnh4MHcBFZntXNFrdvJjX04jRzjzCBOonrkTfj499SZuOh8R33Ls8RRcy5wBgmlkgnY0gmlwhH8AAAGJc2VjcDI1NmsxoQPKY0yuDUmstAHYpMa2_oxVtw0RW_QAdpzBQA8yWM0xOIN1ZHCCdl8");
        var staticHeader = new StaticHeader("test", new byte[32], new byte[32], 0, new byte[32]);
        
        // Arrange
        mockRequestManager
            .Setup(x => x.GetCachedHandshakeInteraction(It.IsAny<byte[]>()))
            .Returns(new byte[32]);
        mockRequestManager
            .Setup(x => x.GetCachedRequest(It.IsAny<byte[]>()))
            .Returns((CachedRequest?)null);
        mockRequestManager
            .Setup(x => x.GetPendingRequestByNodeId(It.IsAny<byte[]>()))
            .Returns((PendingRequest?)null);
        mockPacketBuilder
            .Setup(x => x.BuildRandomOrdinaryPacket(It.IsAny<byte[]>()))
            .Returns(new Tuple<byte[], StaticHeader>(new byte[32], staticHeader));
        mockRoutingTable
            .Setup(x => x.GetNodeEntry(It.IsAny<byte[]>()))
            .Returns(new NodeTableEntry(enrRecord,new IdentitySchemeV4Verifier()));
        mockSessionManager
            .Setup(x => x.GetSession(It.IsAny<byte[]>(), It.IsAny<IPEndPoint>()))
            .Returns(mockSessionMain.Object);
        mockPacketProcessor
            .Setup(x => x.GetStaticHeader(It.IsAny<byte[]>()))
            .Returns(staticHeader);
        
        var handler = new WhoAreYouPacketHandler(mockIdentityManager.Object,mockSessionManager.Object, mockRoutingTable.Object,
            mockRequestManager.Object, mockUdpConnection.Object, mockPacketBuilder.Object, 
            mockPacketProcessor.Object, mockLoggerFactory.Object);
        var fakeResult = new UdpReceiveResult(new byte[32], new IPEndPoint(IPAddress.Parse("18.223.219.100"), 9009));

        // Act
        await handler.HandlePacket(fakeResult);
        
        mockRequestManager
            .Verify(x => x.GetCachedHandshakeInteraction(It.IsAny<byte[]>()), Times.Once);
        mockRoutingTable
            .Verify(x => x.GetNodeEntry(It.IsAny<byte[]>()), Times.Once);
        mockSessionManager
            .Verify(x=> x.GetSession(It.IsAny<byte[]>(), It.IsAny<IPEndPoint>()), Times.Once);
        mockRequestManager
            .Verify(x => x.GetCachedRequest(It.IsAny<byte[]>()), Times.Once);
        mockRequestManager
            .Verify(x => x.GetPendingRequestByNodeId(It.IsAny<byte[]>()), Times.Once);
        mockPacketBuilder
            .Verify(x => x.BuildRandomOrdinaryPacket(It.IsAny<byte[]>()), Times.Once);
        mockUdpConnection
            .Verify(x => x.SendAsync(It.IsAny<byte[]>(), It.IsAny<IPEndPoint>()), Times.Once);
        mockSessionMain
            .Verify(x => x.GenerateIdSignature(It.IsAny<byte[]>()), Times.Never);
    }
    
    [Test]
    public async Task Test_HandlePacket_ShouldReturn_WhenEncryptedMessageIsNull()
    {
        // Test data
        var enrRecord = new EnrRecordFactory().CreateFromString("enr:-IS4QHCYrYZbAKWCBRlAy5zzaDZXJBGkcnh4MHcBFZntXNFrdvJjX04jRzjzCBOonrkTfj499SZuOh8R33Ls8RRcy5wBgmlkgnY0gmlwhH8AAAGJc2VjcDI1NmsxoQPKY0yuDUmstAHYpMa2_oxVtw0RW_QAdpzBQA8yWM0xOIN1ZHCCdl8");
        var staticHeader = new StaticHeader("test", new byte[32], new byte[32], 0, new byte[32]);
        
        // Arrange
        mockRequestManager
            .Setup(x => x.GetCachedHandshakeInteraction(It.IsAny<byte[]>()))
            .Returns(new byte[32]);
        mockRequestManager
            .Setup(x => x.GetCachedRequest(It.IsAny<byte[]>()))
            .Returns((CachedRequest?)null);
        mockRequestManager
            .Setup(x => x.GetPendingRequestByNodeId(It.IsAny<byte[]>()))
            .Returns(new PendingRequest(new byte[32], new PingMessage(2)));
        mockPacketBuilder
            .Setup(x => x.BuildHandshakePacket(It.IsAny<byte[]>(), It.IsAny<byte[]>(), It.IsAny<byte[]>(), It.IsAny<byte[]>(), It.IsAny<byte[]>()))
            .Returns(new Tuple<byte[], StaticHeader>(new byte[32], staticHeader));
        mockRoutingTable
            .Setup(x => x.GetNodeEntry(It.IsAny<byte[]>()))
            .Returns(new NodeTableEntry(enrRecord,new IdentitySchemeV4Verifier()));
        mockSessionMain
            .Setup(x => x.GenerateIdSignature(It.IsAny<byte[]>()))
            .Returns(new byte[32]);
        mockSessionMain
            .Setup(x => x.EncryptMessageWithNewKeys(It.IsAny<EnrRecord>(), It.IsAny<StaticHeader>(), It.IsAny<byte[]>(),
                It.IsAny<byte[]>(), It.IsAny<byte[]>()))
            .Returns((byte[]?)null);
        mockSessionManager
            .Setup(x => x.GetSession(It.IsAny<byte[]>(), It.IsAny<IPEndPoint>()))
            .Returns(mockSessionMain.Object);
        mockPacketProcessor
            .Setup(x => x.GetStaticHeader(It.IsAny<byte[]>()))
            .Returns(staticHeader);
        
        var handler = new WhoAreYouPacketHandler(mockIdentityManager.Object,mockSessionManager.Object, mockRoutingTable.Object,
            mockRequestManager.Object, mockUdpConnection.Object, mockPacketBuilder.Object, 
            mockPacketProcessor.Object, mockLoggerFactory.Object);
        var fakeResult = new UdpReceiveResult(new byte[32], new IPEndPoint(IPAddress.Parse("18.223.219.100"), 9000));

        // Act
        await handler.HandlePacket(fakeResult);
        
        mockRequestManager
            .Verify(x => x.GetCachedHandshakeInteraction(It.IsAny<byte[]>()), Times.Once);
        mockRoutingTable
            .Verify(x => x.GetNodeEntry(It.IsAny<byte[]>()), Times.Once);
        mockSessionManager
            .Verify(x=> x.GetSession(It.IsAny<byte[]>(), It.IsAny<IPEndPoint>()), Times.Once);
        mockRequestManager
            .Verify(x => x.GetCachedRequest(It.IsAny<byte[]>()), Times.Once);
        mockRequestManager
            .Verify(x => x.GetPendingRequestByNodeId(It.IsAny<byte[]>()), Times.Once);
        mockSessionMain
            .Verify(x => x.GenerateIdSignature(It.IsAny<byte[]>()), Times.Once);
        mockSessionMain
            .Verify(x => x.EncryptMessageWithNewKeys(It.IsAny<EnrRecord>(), It.IsAny<StaticHeader>(), It.IsAny<byte[]>(), It.IsAny<byte[]>(), It.IsAny<byte[]>()), Times.Once);
        mockUdpConnection
            .Verify(x => x.SendAsync(It.IsAny<byte[]>(), It.IsAny<IPEndPoint>()), Times.Never);
    }
    
     [Test]
    public async Task Test_HandlePacket_ShouldReturn_WhenIdSignatureIsNull()
    {
        // Test data
        var enrRecord = new EnrRecordFactory().CreateFromString("enr:-IS4QHCYrYZbAKWCBRlAy5zzaDZXJBGkcnh4MHcBFZntXNFrdvJjX04jRzjzCBOonrkTfj499SZuOh8R33Ls8RRcy5wBgmlkgnY0gmlwhH8AAAGJc2VjcDI1NmsxoQPKY0yuDUmstAHYpMa2_oxVtw0RW_QAdpzBQA8yWM0xOIN1ZHCCdl8");
        var staticHeader = new StaticHeader("test", new byte[32], new byte[32], 0, new byte[32]);
        
        // Arrange
        mockRequestManager
            .Setup(x => x.GetCachedHandshakeInteraction(It.IsAny<byte[]>()))
            .Returns(new byte[32]);
        mockRequestManager
            .Setup(x => x.GetCachedRequest(It.IsAny<byte[]>()))
            .Returns((CachedRequest?)null);
        mockRequestManager
            .Setup(x => x.GetPendingRequestByNodeId(It.IsAny<byte[]>()))
            .Returns(new PendingRequest(new byte[32], new PingMessage(2)));
        mockRoutingTable
            .Setup(x => x.GetNodeEntry(It.IsAny<byte[]>()))
            .Returns(new NodeTableEntry(enrRecord,new IdentitySchemeV4Verifier()));
        mockSessionMain
            .Setup(x => x.GenerateIdSignature(It.IsAny<byte[]>()))
            .Returns((byte[]?)null);
        mockSessionMain
            .Setup(x => x.EncryptMessageWithNewKeys(It.IsAny<EnrRecord>(), It.IsAny<StaticHeader>(), It.IsAny<byte[]>(),
                It.IsAny<byte[]>(), It.IsAny<byte[]>()))
            .Returns((byte[]?)null);
        mockSessionManager
            .Setup(x => x.GetSession(It.IsAny<byte[]>(), It.IsAny<IPEndPoint>()))
            .Returns(mockSessionMain.Object);
        mockPacketProcessor
            .Setup(x => x.GetStaticHeader(It.IsAny<byte[]>()))
            .Returns(staticHeader);
        
        var handler = new WhoAreYouPacketHandler(mockIdentityManager.Object,mockSessionManager.Object, mockRoutingTable.Object,
            mockRequestManager.Object, mockUdpConnection.Object, mockPacketBuilder.Object, 
            mockPacketProcessor.Object, mockLoggerFactory.Object);
        var fakeResult = new UdpReceiveResult(new byte[32], new IPEndPoint(IPAddress.Parse("18.223.219.100"), 9000));

        // Act
        await handler.HandlePacket(fakeResult);
        
        mockRequestManager
            .Verify(x => x.GetCachedHandshakeInteraction(It.IsAny<byte[]>()), Times.Once);
        mockRoutingTable
            .Verify(x => x.GetNodeEntry(It.IsAny<byte[]>()), Times.Once);
        mockSessionManager
            .Verify(x=> x.GetSession(It.IsAny<byte[]>(), It.IsAny<IPEndPoint>()), Times.Once);
        mockRequestManager
            .Verify(x => x.GetCachedRequest(It.IsAny<byte[]>()), Times.Once);
        mockRequestManager
            .Verify(x => x.GetPendingRequestByNodeId(It.IsAny<byte[]>()), Times.Once);
        mockSessionMain
            .Verify(x => x.GenerateIdSignature(It.IsAny<byte[]>()), Times.Once);
        mockSessionMain
            .Verify(x => x.EncryptMessageWithNewKeys(It.IsAny<EnrRecord>(), It.IsAny<StaticHeader>(), It.IsAny<byte[]>(), It.IsAny<byte[]>(), It.IsAny<byte[]>()), Times.Never);

    }
    
    [Test]
    public async Task Test_HandlePacket_ShouldSendPacket_WhenEncryptedMessageIsNotNull()
    {
        // Test data
        var enrRecord = new EnrRecordFactory().CreateFromString("enr:-IS4QHCYrYZbAKWCBRlAy5zzaDZXJBGkcnh4MHcBFZntXNFrdvJjX04jRzjzCBOonrkTfj499SZuOh8R33Ls8RRcy5wBgmlkgnY0gmlwhH8AAAGJc2VjcDI1NmsxoQPKY0yuDUmstAHYpMa2_oxVtw0RW_QAdpzBQA8yWM0xOIN1ZHCCdl8");
        var staticHeader = new StaticHeader("test", new byte[32], new byte[32], 0, new byte[32]);
        
        // Arrange
        mockRequestManager
            .Setup(x => x.GetCachedHandshakeInteraction(It.IsAny<byte[]>()))
            .Returns(new byte[32]);
        mockRequestManager
            .Setup(x => x.GetCachedRequest(It.IsAny<byte[]>()))
            .Returns((CachedRequest?)null);
        mockRequestManager
            .Setup(x => x.GetPendingRequestByNodeId(It.IsAny<byte[]>()))
            .Returns(new PendingRequest(new byte[32], new PingMessage(2)));
        mockPacketBuilder
            .Setup(x => x.BuildHandshakePacket(It.IsAny<byte[]>(), It.IsAny<byte[]>(), It.IsAny<byte[]>(), It.IsAny<byte[]>(), It.IsAny<byte[]>()))
            .Returns(new Tuple<byte[], StaticHeader>(new byte[32], staticHeader));
        mockRoutingTable
            .Setup(x => x.GetNodeEntry(It.IsAny<byte[]>()))
            .Returns(new NodeTableEntry(enrRecord,new IdentitySchemeV4Verifier()));
        mockSessionMain
            .Setup(x => x.GenerateIdSignature(It.IsAny<byte[]>()))
            .Returns(new byte[32]);
        mockSessionMain
            .Setup(x => x.EncryptMessageWithNewKeys(It.IsAny<EnrRecord>(), It.IsAny<StaticHeader>(), It.IsAny<byte[]>(),
                It.IsAny<byte[]>(), It.IsAny<byte[]>()))
            .Returns(new byte[32]);
        mockSessionManager
            .Setup(x => x.GetSession(It.IsAny<byte[]>(), It.IsAny<IPEndPoint>()))
            .Returns(mockSessionMain.Object);
        mockPacketProcessor
            .Setup(x => x.GetStaticHeader(It.IsAny<byte[]>()))
            .Returns(staticHeader);
        
        var handler = new WhoAreYouPacketHandler(mockIdentityManager.Object,mockSessionManager.Object, mockRoutingTable.Object,
            mockRequestManager.Object, mockUdpConnection.Object, mockPacketBuilder.Object, 
            mockPacketProcessor.Object, mockLoggerFactory.Object);
        var fakeResult = new UdpReceiveResult(new byte[32], new IPEndPoint(IPAddress.Parse("18.223.219.100"), 9000));

        // Act
        await handler.HandlePacket(fakeResult);
        
        mockRequestManager
            .Verify(x => x.GetCachedHandshakeInteraction(It.IsAny<byte[]>()), Times.Once);
        mockRoutingTable
            .Verify(x => x.GetNodeEntry(It.IsAny<byte[]>()), Times.Once);
        mockSessionManager
            .Verify(x=> x.GetSession(It.IsAny<byte[]>(), It.IsAny<IPEndPoint>()), Times.Once);
        mockRequestManager
            .Verify(x => x.GetCachedRequest(It.IsAny<byte[]>()), Times.Once);
        mockRequestManager
            .Verify(x => x.GetPendingRequestByNodeId(It.IsAny<byte[]>()), Times.Once);
        mockSessionMain
            .Verify(x => x.GenerateIdSignature(It.IsAny<byte[]>()), Times.Once);
        mockSessionMain
            .Verify(x => x.EncryptMessageWithNewKeys(It.IsAny<EnrRecord>(), It.IsAny<StaticHeader>(), It.IsAny<byte[]>(), It.IsAny<byte[]>(), It.IsAny<byte[]>()), Times.Once);
        mockUdpConnection
            .Verify(x => x.SendAsync(It.IsAny<byte[]>(), It.IsAny<IPEndPoint>()), Times.Once);
    }
    
    
}