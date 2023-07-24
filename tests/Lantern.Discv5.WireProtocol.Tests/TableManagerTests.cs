
using Lantern.Discv5.Enr;
using Lantern.Discv5.Enr.EnrFactory;
using Lantern.Discv5.WireProtocol.Message;
using Lantern.Discv5.WireProtocol.Packet;
using Lantern.Discv5.WireProtocol.Table;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace Lantern.Discv5.WireProtocol.Tests;

public class TableManagerTests
{
    private Mock<IPacketManager> mockPacketManager = null!;
    private Mock<ILookupManager> mockLookupManager = null!;
    private Mock<IRoutingTable> mockRoutingTable = null!;
    private Mock<ILogger<TableManager>> mockLogger = null!;
    private Mock<ILoggerFactory> mockLoggerFactory = null!;
    private TableOptions tableOptions = null!;

    [SetUp]
    public void Init()
    {
        mockPacketManager = new Mock<IPacketManager>();
        mockLookupManager = new Mock<ILookupManager>();
        mockRoutingTable = new Mock<IRoutingTable>();
        mockLogger = new Mock<ILogger<TableManager>>();
        tableOptions = TableOptions.Default;
        mockLoggerFactory = new Mock<ILoggerFactory>();
        mockLoggerFactory
            .Setup(x => x.CreateLogger(It.IsAny<string>()))
            .Returns(mockLogger.Object);
    }

    [Test]
    public async Task Test_TableManager_EnsureShutdownTokenIsRequested_WhenTableManagerIsStopped()
    {
        tableOptions = new TableOptions.Builder()
            .WithLookupTimeoutMilliseconds(100)
            .WithPingIntervalMilliseconds(100)
            .WithRefreshIntervalMilliseconds(100)
            .Build();

        mockRoutingTable
            .Setup(x => x.GetTotalEntriesCount())
            .Returns(10);

        var tableManager = new TableManager(mockPacketManager.Object, mockLookupManager.Object, mockRoutingTable.Object,
            mockLoggerFactory.Object, tableOptions);

        tableManager.StartTableManagerAsync();
        await tableManager.StopTableManagerAsync();

        Assert.IsTrue(tableManager.ShutdownCts.IsCancellationRequested);
    }

    [Test]
    public async Task Test_TableManager_PingNodeAsync()
    {
        tableOptions = new TableOptions.Builder()
            .WithLookupTimeoutMilliseconds(100)
            .WithPingIntervalMilliseconds(100)
            .WithRefreshIntervalMilliseconds(100)
            .Build();
        
        mockRoutingTable
            .Setup(x => x.GetTotalEntriesCount())
            .Returns(10);
        
        var tableManager = new TableManager(mockPacketManager.Object, mockLookupManager.Object, mockRoutingTable.Object,
            mockLoggerFactory.Object, tableOptions);
        
        tableManager.StartTableManagerAsync();
        await tableManager.PingNodeAsync();
    }
    
}