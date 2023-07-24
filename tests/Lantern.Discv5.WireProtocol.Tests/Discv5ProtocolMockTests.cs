using Lantern.Discv5.Enr.EnrFactory;
using Lantern.Discv5.Enr.IdentityScheme.V4;
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
        var enrRecord = new EnrRecordFactory().CreateFromString("enr:-IS4QHCYrYZbAKWCBRlAy5zzaDZXJBGkcnh4MHcBFZntXNFrdvJjX04jRzjzCBOonrkTfj499SZuOh8R33Ls8RRcy5wBgmlkgnY0gmlwhH8AAAGJc2VjcDI1NmsxoQPKY0yuDUmstAHYpMa2_oxVtw0RW_QAdpzBQA8yWM0xOIN1ZHCCdl8");
        mockIdentityManager
            .Setup(x => x.Record)
            .Returns(enrRecord);
        
        SetupServices();
        var result = _discv5Protocol.SelfEnrRecord;
        Assert.AreEqual(result, enrRecord);
    }
    
    [Test]
    public void ShouldReturnNodesCount()
    {
        mockRoutingTable
            .Setup(x => x.GetTotalEntriesCount())
            .Returns(10);
        
        SetupServices();
        var result = _discv5Protocol.NodesCount;
        Assert.AreEqual(result, 10);
    }
    
    [Test]
    public void ShouldReturnPeerCount()
    {
        mockRoutingTable
            .Setup(x => x.GetTotalActiveNodesCount())
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
        var enrRecord = new EnrRecordFactory().CreateFromString("enr:-IS4QHCYrYZbAKWCBRlAy5zzaDZXJBGkcnh4MHcBFZntXNFrdvJjX04jRzjzCBOonrkTfj499SZuOh8R33Ls8RRcy5wBgmlkgnY0gmlwhH8AAAGJc2VjcDI1NmsxoQPKY0yuDUmstAHYpMa2_oxVtw0RW_QAdpzBQA8yWM0xOIN1ZHCCdl8");
        var nodeEntry = new NodeTableEntry(enrRecord, new IdentitySchemeV4Verifier());
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
        var enrRecord = new EnrRecordFactory().CreateFromString("enr:-IS4QHCYrYZbAKWCBRlAy5zzaDZXJBGkcnh4MHcBFZntXNFrdvJjX04jRzjzCBOonrkTfj499SZuOh8R33Ls8RRcy5wBgmlkgnY0gmlwhH8AAAGJc2VjcDI1NmsxoQPKY0yuDUmstAHYpMa2_oxVtw0RW_QAdpzBQA8yWM0xOIN1ZHCCdl8");
        var nodeEntry = new NodeTableEntry(enrRecord, new IdentitySchemeV4Verifier());
        
        mockRoutingTable
            .Setup(x => x.GetAllNodeEntries())
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
            .Setup(x => x.GetTotalEntriesCount())
            .Returns(0);
        
        SetupServices();
        var result = await _discv5Protocol.PerformLookupAsync(RandomUtility.GenerateRandomData(32));
        Assert.IsNull(result);
    }

    private void SetupServices()
    {
        _serviceProviderMock.Setup(sp => sp.GetService(typeof(IConnectionManager))).Returns(mockConnectionManager.Object);
        _serviceProviderMock.Setup(sp => sp.GetService(typeof(ITableManager))).Returns(mockTableManager.Object);
        _serviceProviderMock.Setup(sp => sp.GetService(typeof(IRequestManager))).Returns(mockRequestManager.Object);
        _serviceProviderMock.Setup(sp => sp.GetService(typeof(IPacketManager))).Returns(mockPacketManager.Object);
        _serviceProviderMock.Setup(sp => sp.GetService(typeof(IRoutingTable))).Returns(mockRoutingTable.Object);
        _serviceProviderMock.Setup(sp => sp.GetService(typeof(ISessionManager))).Returns(mockSessionManager.Object);
        _serviceProviderMock.Setup(sp => sp.GetService(typeof(ILookupManager))).Returns(mockLookupManager.Object);
        _serviceProviderMock.Setup(sp => sp.GetService(typeof(IIdentityManager))).Returns(mockIdentityManager.Object);
        _serviceProviderMock.Setup(sp => sp.GetService(typeof(ILoggerFactory))).Returns(mockLoggerFactory.Object);
        _discv5Protocol = new Discv5Protocol(_serviceProviderMock.Object);
        _discv5Protocol.StartProtocol();
    }
}