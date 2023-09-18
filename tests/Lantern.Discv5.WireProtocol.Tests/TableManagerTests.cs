using Lantern.Discv5.Enr;
using Lantern.Discv5.WireProtocol.Identity;
using Lantern.Discv5.WireProtocol.Packet;
using Lantern.Discv5.WireProtocol.Table;
using Lantern.Discv5.WireProtocol.Utility;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace Lantern.Discv5.WireProtocol.Tests;

public class TableManagerTests
{
    private Mock<IPacketManager> mockPacketManager = null!;
    private Mock<IEnrFactory> mockEnrRecordFactory = null!;
    private Mock<IIdentityManager> mockIdentityManager = null!;
    private Mock<ILookupManager> mockLookupManager = null!;
    private Mock<IRoutingTable> mockRoutingTable = null!;
    private Mock<ICancellationTokenSourceWrapper> mockCancellationTokenSource = null!;
    private Mock<IGracefulTaskRunner> mockGracefulTaskRunner = null!;
    private Mock<ILogger<TableManager>> mockLogger = null!;
    private Mock<ILoggerFactory> mockLoggerFactory = null!;
    private TableOptions tableOptions = null!;

    [SetUp]
    public void Init()
    {
        mockPacketManager = new Mock<IPacketManager>();
        mockIdentityManager = new Mock<IIdentityManager>();
        mockEnrRecordFactory = new Mock<IEnrFactory>();
        mockLookupManager = new Mock<ILookupManager>();
        mockCancellationTokenSource = new Mock<ICancellationTokenSourceWrapper>();
        mockGracefulTaskRunner = new Mock<IGracefulTaskRunner>();
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

        var tableManager = new TableManager(mockPacketManager.Object, mockIdentityManager.Object, mockLookupManager.Object, mockRoutingTable.Object, mockEnrRecordFactory.Object,mockLoggerFactory.Object, mockCancellationTokenSource.Object, mockGracefulTaskRunner.Object, tableOptions);

        tableManager.StartTableManagerAsync();
        await tableManager.StopTableManagerAsync();

        mockCancellationTokenSource.Verify(x => x.Cancel(), Times.Once);
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
        
        var tableManager = new TableManager(mockPacketManager.Object, mockIdentityManager.Object, mockLookupManager.Object, mockRoutingTable.Object, mockEnrRecordFactory.Object,mockLoggerFactory.Object, mockCancellationTokenSource.Object, mockGracefulTaskRunner.Object, tableOptions);

        tableManager.StartTableManagerAsync();
        await tableManager.StopTableManagerAsync();
    }
}