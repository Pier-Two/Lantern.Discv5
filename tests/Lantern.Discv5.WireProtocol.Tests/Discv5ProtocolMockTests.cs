using Lantern.Discv5.Enr;
using Lantern.Discv5.Enr.Identity.V4;
using Lantern.Discv5.WireProtocol.Connection;
using Lantern.Discv5.WireProtocol.Identity;
using Lantern.Discv5.WireProtocol.Message;
using Lantern.Discv5.WireProtocol.Packet;
using Lantern.Discv5.WireProtocol.Session;
using Lantern.Discv5.WireProtocol.Table;
using Lantern.Discv5.WireProtocol.Utility;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace Lantern.Discv5.WireProtocol.Tests;

public class Discv5ProtocolMockTests
{
    private Discv5Protocol _discv5Protocol;
    private Mock<IServiceProvider> _serviceProviderMock;
    private Mock<IConnectionManager> mockConnectionManager = null!;
    private Mock<ITableManager> mockTableManager = null!;
    private Mock<IRequestManager> mockRequestManager = null!;
    private Mock<IPacketManager> mockPacketManager = null!;
    private Mock<IRoutingTable> mockRoutingTable = null!;
    private Mock<ISessionManager> mockSessionManager = null!;
    private Mock<ILookupManager> mockLookupManager = null!;
    private Mock<IIdentityManager> mockIdentityManager = null!;
    private Mock<ILogger<Discv5Protocol>> mockLogger = null!;
    private Mock<ILoggerFactory> mockLoggerFactory = null!;

    [SetUp]
    public void Init()
    {
        _serviceProviderMock = new Mock<IServiceProvider>();
        mockConnectionManager = new Mock<IConnectionManager>();
        mockTableManager = new Mock<ITableManager>();
        mockRequestManager = new Mock<IRequestManager>();
        mockPacketManager = new Mock<IPacketManager>();
        mockRoutingTable = new Mock<IRoutingTable>();
        mockSessionManager = new Mock<ISessionManager>();
        mockLookupManager = new Mock<ILookupManager>();
        mockIdentityManager = new Mock<IIdentityManager>();
        mockLogger = new Mock<ILogger<Discv5Protocol>>();
        mockLoggerFactory = new Mock<ILoggerFactory>();
        mockLoggerFactory
            .Setup(x => x.CreateLogger(It.IsAny<string>()))
            .Returns(mockLogger.Object);
    }



    [Test]
    public void StartProtocol_InvokesStartMethodsOnServices()
    {
        SetupServices();
        mockConnectionManager.Verify(cm => cm.StartConnectionManagerAsync(), Times.Once);
        mockTableManager.Verify(tm => tm.StartTableManagerAsync(), Times.Once);
        mockRequestManager.Verify(rm => rm.StartRequestManager(), Times.Once);
    }
    
    [Test]
    public async Task StopProtocolAsync_InvokesStopMethodsOnServices()
    {
        SetupServices();
        await _discv5Protocol.StopProtocolAsync();
        mockConnectionManager.Verify(cm => cm.StopConnectionManagerAsync(), Times.Once);
        mockTableManager.Verify(tm => tm.StopTableManagerAsync(), Times.Once);
        mockRequestManager.Verify(rm => rm.StopRequestManagerAsync(), Times.Once);
    }
    
    [Test]
    public void ShouldReturnSelfEnrRecord()
    {
        var enrEntryRegistry = new EnrEntryRegistry();
        var enrRecord = new EnrFactory(enrEntryRegistry).CreateFromString("enr:-IS4QHCYrYZbAKWCBRlAy5zzaDZXJBGkcnh4MHcBFZntXNFrdvJjX04jRzjzCBOonrkTfj499SZuOh8R33Ls8RRcy5wBgmlkgnY0gmlwhH8AAAGJc2VjcDI1NmsxoQPKY0yuDUmstAHYpMa2_oxVtw0RW_QAdpzBQA8yWM0xOIN1ZHCCdl8", new IdentityVerifierV4());
        mockIdentityManager
            .Setup(x => x.Record)
            .Returns(enrRecord);
        
        SetupServices();
        var result = _discv5Protocol.SelfEnr;
        Assert.AreEqual(result, enrRecord);
    }
    
    [Test]
    public void ShouldReturnNodesCount()
    {
        mockRoutingTable
            .Setup(x => x.GetNodesCount())
            .Returns(10);
        
        SetupServices();
        var result = _discv5Protocol.NodesCount;
        Assert.AreEqual(result, 10);
    }
    
    [Test]
    public void ShouldReturnPeerCount()
    {
        mockRoutingTable
            .Setup(x => x.GetActiveNodesCount())
            .Returns(10);
        
        SetupServices();
        var result = _discv5Protocol.PeerCount;
        Assert.AreEqual(result, 10);
    }
    
    [Test]
    public void ShouldReturnActiveSessionCount()
    {
        mockSessionManager
            .Setup(x => x.TotalSessionCount)
            .Returns(10);
        
        SetupServices();
        var result = _discv5Protocol.ActiveSessionCount;
        Assert.AreEqual(result, 10);
    }
    
    [Test]
    public void ShouldReturnNodeFromId()
    {
        var enrEntryRegistry = new EnrEntryRegistry();
        var enrRecord = new EnrFactory(enrEntryRegistry).CreateFromString("enr:-IS4QHCYrYZbAKWCBRlAy5zzaDZXJBGkcnh4MHcBFZntXNFrdvJjX04jRzjzCBOonrkTfj499SZuOh8R33Ls8RRcy5wBgmlkgnY0gmlwhH8AAAGJc2VjcDI1NmsxoQPKY0yuDUmstAHYpMa2_oxVtw0RW_QAdpzBQA8yWM0xOIN1ZHCCdl8", new IdentityVerifierV4());
        var nodeEntry = new NodeTableEntry(enrRecord, new IdentityVerifierV4());
        mockRoutingTable
            .Setup(x => x.GetNodeEntry(nodeEntry.Id))
            .Returns(nodeEntry);
        
        SetupServices();
        var result = _discv5Protocol.GetNodeFromId(nodeEntry.Id);
        Assert.IsTrue(result.Id.SequenceEqual(nodeEntry.Id));
    }

    [Test]
    public void ShouldReturnAllNodes()
    {
        var enrEntryRegistry = new EnrEntryRegistry();
        var enrRecord = new EnrFactory(enrEntryRegistry).CreateFromString("enr:-IS4QHCYrYZbAKWCBRlAy5zzaDZXJBGkcnh4MHcBFZntXNFrdvJjX04jRzjzCBOonrkTfj499SZuOh8R33Ls8RRcy5wBgmlkgnY0gmlwhH8AAAGJc2VjcDI1NmsxoQPKY0yuDUmstAHYpMa2_oxVtw0RW_QAdpzBQA8yWM0xOIN1ZHCCdl8", new IdentityVerifierV4());
        var nodeEntry = new NodeTableEntry(enrRecord, new IdentityVerifierV4());
        
        mockRoutingTable
            .Setup(x => x.GetAllNodes())
            .Returns(new [] { nodeEntry });
        
        SetupServices();
        var result = _discv5Protocol.GetAllNodes();
        Assert.IsTrue(result.Length == 1);
        Assert.IsTrue(result[0].Id.SequenceEqual(nodeEntry.Id));
    }

    [Test]
    public async Task PerformLookupAsync_ShouldReturnNull_WhenNoActiveNodes()
    {
        mockRoutingTable
            .Setup(x => x.GetNodesCount())
            .Returns(0);
        
        SetupServices();
        var result = await _discv5Protocol.PerformLookupAsync(RandomUtility.GenerateRandomData(32));
        Assert.IsNull(result);
    }
    
    [Test]
    public async Task SendPingAsync_ShouldReturnTrue_WhenNoExceptionIsThrown()
    {
        // Arrange
        var enrEntryRegistry = new EnrEntryRegistry();
        var enrRecord = new EnrFactory(enrEntryRegistry).CreateFromString("enr:-IS4QHCYrYZbAKWCBRlAy5zzaDZXJBGkcnh4MHcBFZntXNFrdvJjX04jRzjzCBOonrkTfj499SZuOh8R33Ls8RRcy5wBgmlkgnY0gmlwhH8AAAGJc2VjcDI1NmsxoQPKY0yuDUmstAHYpMa2_oxVtw0RW_QAdpzBQA8yWM0xOIN1ZHCCdl8", new IdentityVerifierV4());
        SetupServices();

        // Act
        var result  = await _discv5Protocol.SendPingAsync(enrRecord);
        mockPacketManager.Verify(x => x.SendPacket(enrRecord, MessageType.Ping, It.IsAny<byte[][]>()), Times.Once);
        Assert.IsTrue(result);
    }

    [Test]
    public async Task SendPingAsync_ShouldReturnFalse_WhenExceptionThrown()
    {
        // Arrange
        var enrEntryRegistry = new EnrEntryRegistry();
        var enrRecord = new EnrFactory(enrEntryRegistry).CreateFromString("enr:-IS4QHCYrYZbAKWCBRlAy5zzaDZXJBGkcnh4MHcBFZntXNFrdvJjX04jRzjzCBOonrkTfj499SZuOh8R33Ls8RRcy5wBgmlkgnY0gmlwhH8AAAGJc2VjcDI1NmsxoQPKY0yuDUmstAHYpMa2_oxVtw0RW_QAdpzBQA8yWM0xOIN1ZHCCdl8", new IdentityVerifierV4());
        var exceptionToThrow = new Exception("Test exception");
    
        mockPacketManager
            .Setup(x => x.SendPacket(It.IsAny<Enr.Enr>(), It.IsAny<MessageType>(), It.IsAny<byte[][]>()))
            .ThrowsAsync(exceptionToThrow);
        
        SetupServices();

        // Act
        var result  = await _discv5Protocol.SendPingAsync(enrRecord);
        mockPacketManager.Verify(x => x.SendPacket(enrRecord, MessageType.Ping, It.IsAny<byte[][]>()), Times.Once);
        Assert.IsFalse(result);
    }

    [Test]
    public async Task SendFindNodeAsync_ShouldReturnTrue_WhenNoExceptionIsThrown()
    {
        var enrEntryRegistry = new EnrEntryRegistry();
        var enrRecord = new EnrFactory(enrEntryRegistry).CreateFromString("enr:-IS4QHCYrYZbAKWCBRlAy5zzaDZXJBGkcnh4MHcBFZntXNFrdvJjX04jRzjzCBOonrkTfj499SZuOh8R33Ls8RRcy5wBgmlkgnY0gmlwhH8AAAGJc2VjcDI1NmsxoQPKY0yuDUmstAHYpMa2_oxVtw0RW_QAdpzBQA8yWM0xOIN1ZHCCdl8", new IdentityVerifierV4());
        
        mockPacketManager
            .Setup(x => x.SendPacket(It.IsAny<Enr.Enr>(), It.IsAny<MessageType>(), It.IsAny<byte[][]>()))
            .Returns(Task.CompletedTask);
        
        SetupServices();
        
        var result = await _discv5Protocol.SendFindNodeAsync(enrRecord, RandomUtility.GenerateRandomData(32));
        mockPacketManager.Verify(x => x.SendPacket(enrRecord, MessageType.FindNode, It.IsAny<byte[][]>()), Times.Once);
        Assert.IsTrue(result);
    }
    
    [Test]
    public async Task SendFindNodeAsync_ShouldReturnFalse_WhenExceptionIsThrown()
    {
        var enrEntryRegistry = new EnrEntryRegistry();
        var enrRecord = new EnrFactory(enrEntryRegistry).CreateFromString("enr:-IS4QHCYrYZbAKWCBRlAy5zzaDZXJBGkcnh4MHcBFZntXNFrdvJjX04jRzjzCBOonrkTfj499SZuOh8R33Ls8RRcy5wBgmlkgnY0gmlwhH8AAAGJc2VjcDI1NmsxoQPKY0yuDUmstAHYpMa2_oxVtw0RW_QAdpzBQA8yWM0xOIN1ZHCCdl8", new IdentityVerifierV4());
        var exceptionToThrow = new Exception("Test exception");
    
        mockPacketManager
            .Setup(x => x.SendPacket(It.IsAny<Enr.Enr>(), It.IsAny<MessageType>(), It.IsAny<byte[][]>()))
            .ThrowsAsync(exceptionToThrow);

        SetupServices();
        
        var result = await _discv5Protocol.SendFindNodeAsync(enrRecord, RandomUtility.GenerateRandomData(32));
        mockPacketManager.Verify(x => x.SendPacket(enrRecord, MessageType.FindNode, It.IsAny<byte[][]>()), Times.Once);
        Assert.False(result);
    }

    [Test]
    public async Task SendTalkReqAsync_ShouldReturnTrue_WhenNoExceptionIsThrown()
    {
        var enrEntryRegistry = new EnrEntryRegistry();
        var enrRecord = new EnrFactory(enrEntryRegistry).CreateFromString("enr:-IS4QHCYrYZbAKWCBRlAy5zzaDZXJBGkcnh4MHcBFZntXNFrdvJjX04jRzjzCBOonrkTfj499SZuOh8R33Ls8RRcy5wBgmlkgnY0gmlwhH8AAAGJc2VjcDI1NmsxoQPKY0yuDUmstAHYpMa2_oxVtw0RW_QAdpzBQA8yWM0xOIN1ZHCCdl8", new IdentityVerifierV4());
        
        mockPacketManager
            .Setup(x => x.SendPacket(It.IsAny<Enr.Enr>(), It.IsAny<MessageType>(), It.IsAny<byte[][]>()))
            .Returns(Task.CompletedTask);
        
        SetupServices();
        var result = await _discv5Protocol.SendTalkReqAsync(enrRecord, RandomUtility.GenerateRandomData(32), RandomUtility.GenerateRandomData(32));
        mockPacketManager.Verify(x => x.SendPacket(enrRecord, MessageType.TalkReq, It.IsAny<byte[][]>()), Times.Once);
        Assert.IsTrue(result);
    }

    [Test]
    public async Task SendTalkReqAsync_ShouldReturnFalse_WhenExceptionIsThrown()
    {
        var enrEntryRegistry = new EnrEntryRegistry();
        var enrRecord = new EnrFactory(enrEntryRegistry).CreateFromString("enr:-IS4QHCYrYZbAKWCBRlAy5zzaDZXJBGkcnh4MHcBFZntXNFrdvJjX04jRzjzCBOonrkTfj499SZuOh8R33Ls8RRcy5wBgmlkgnY0gmlwhH8AAAGJc2VjcDI1NmsxoQPKY0yuDUmstAHYpMa2_oxVtw0RW_QAdpzBQA8yWM0xOIN1ZHCCdl8", new IdentityVerifierV4());
        var exceptionToThrow = new Exception("Test exception");
        
        mockPacketManager
            .Setup(x => x.SendPacket(It.IsAny<Enr.Enr>(), It.IsAny<MessageType>(), It.IsAny<byte[][]>()))
            .ThrowsAsync(exceptionToThrow);

        SetupServices();
        
        var result = await _discv5Protocol.SendTalkReqAsync(enrRecord, RandomUtility.GenerateRandomData(32), RandomUtility.GenerateRandomData(32));
        mockPacketManager.Verify(x => x.SendPacket(enrRecord, MessageType.TalkReq, It.IsAny<byte[][]>()), Times.Once);
        Assert.IsFalse(result);
    }

    private void SetupServices()
    {
        _discv5Protocol = new Discv5Protocol(
            mockConnectionManager.Object,
            mockIdentityManager.Object,
            mockTableManager.Object,
            mockRequestManager.Object,
            mockPacketManager.Object,
            mockRoutingTable.Object,
            mockSessionManager.Object,
            mockLookupManager.Object,
            mockLoggerFactory.Object.CreateLogger<Discv5Protocol>()
        );
        _discv5Protocol.StartProtocolAsync();
    }
}