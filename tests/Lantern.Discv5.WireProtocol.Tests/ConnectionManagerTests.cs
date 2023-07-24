using System.Net;
using System.Net.Sockets;
using System.Text;
using Lantern.Discv5.Enr.EnrContent.Entries;
using Lantern.Discv5.Enr.EnrFactory;
using Lantern.Discv5.Enr.IdentityScheme.V4;
using Lantern.Discv5.WireProtocol.Connection;
using Lantern.Discv5.WireProtocol.Identity;
using Lantern.Discv5.WireProtocol.Packet;
using Lantern.Discv5.WireProtocol.Session;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace Lantern.Discv5.WireProtocol.Tests;

[TestFixture]
public class ConnectionManagerTests
{
    private Mock<IPacketManager> _packetManagerMock;
    private Mock<IUdpConnection> _udpConnectionMock;
    private Mock<ILogger<ConnectionManager>> _loggerMock;
    private Mock<ILoggerFactory> _loggerFactoryMock;
    private ConnectionManager _connectionManager;
    private CancellationTokenSource _source;

    [SetUp]
    public void SetUp()
    {
        _packetManagerMock = new Mock<IPacketManager>();
        _udpConnectionMock = new Mock<IUdpConnection>();
        _loggerMock = new Mock<ILogger<ConnectionManager>>();
        _loggerFactoryMock = new Mock<ILoggerFactory>();
        _loggerFactoryMock.Setup(x => x.CreateLogger(typeof(ConnectionManager).FullName)).Returns(_loggerMock.Object);
        _source = new CancellationTokenSource();
        _connectionManager = new ConnectionManager(_packetManagerMock.Object, _udpConnectionMock.Object, _loggerFactoryMock.Object);
    }

    [Test]
    public void StartConnectionManagerAsync_AssertFunctionsCalled()
    {
        _connectionManager.StartConnectionManagerAsync();
        _udpConnectionMock.Verify(x => x.ListenAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task StopConnectionManagerAsync_AssertsFunctionsCalled()
    {
        _connectionManager.StartConnectionManagerAsync();
        await _connectionManager.StopConnectionManagerAsync();

        _udpConnectionMock.Verify(x => x.CompleteMessageChannel(), Times.Once);
    }

    [TearDown]
    public void TearDown()
    {
        _source.Dispose();
    }
}