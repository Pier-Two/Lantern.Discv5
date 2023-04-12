using System.Net;
using System.Net.Sockets;
using System.Text;
using Lantern.Discv5.WireProtocol.Connection;
using Lantern.Discv5.WireProtocol.Logging.Exceptions;
using NUnit.Framework;

namespace Lantern.Discv5.WireProtocol.Tests;

[TestFixture]
public class UdpConnectionTests
{
    private UdpConnection _sender = null!;
    private UdpConnection _receiver = null!;
    private int _senderPort;
    private int _receiverPort;

    [SetUp]
    public void Setup()
    {
        _senderPort = 1234;
        _receiverPort = 1235;
        _sender = new UdpConnection(_senderPort);
        _receiver = new UdpConnection(_receiverPort);
    }

    [TearDown]
    public void Cleanup()
    {
        _sender.Close();
        _receiver.Close();
    }

    [Test]
    public async Task Test_SendingAndReceivingData_ShouldSendAndReceiveDataCorrectly()
    {
        const string enrString =
            "enr:-IS4QHCYrYZbAKWCBRlAy5zzaDZXJBGkcnh4MHcBFZntXNFrdvJjX04jRzjzCBOonrkTfj499SZuOh8R33Ls8RRcy5wBgmlkgnY0gmlwhH8AAAGJc2VjcDI1NmsxoQPKY0yuDUmstAHYpMa2_oxVtw0RW_QAdpzBQA8yWM0xOIN1ZHCCdl8";
        var data = Encoding.ASCII.GetBytes(enrString);
        var destination = new IPEndPoint(IPAddress.Loopback, _receiverPort);
        await _sender.SendAsync(data, destination);

        var receiveBytes = await _receiver.ReceiveAsync();
        var receiveString = Encoding.ASCII.GetString(receiveBytes);

        Assert.AreEqual(enrString, receiveString);
    }

    [Test]
    public void Test_SendingData_ShouldNotSendLargeData()
    {
        var data = new byte[1281];
        var destination = new IPEndPoint(IPAddress.Loopback, _receiverPort);
        var ex = Assert.ThrowsAsync<InvalidPacketException>(() => _sender.SendAsync(data, destination));
        Assert.AreEqual("Packet is too large", ex!.Message);
    }

    [Test]
    public void Test_SendingData_ShouldNotSendSmallData()
    {
        var data = new byte[62];
        var destination = new IPEndPoint(IPAddress.Loopback, _receiverPort);
        var ex = Assert.ThrowsAsync<InvalidPacketException>(() => _sender.SendAsync(data, destination));
        Assert.AreEqual("Packet is too small", ex!.Message);
    }

    [Test]
    public async Task Test_ReceivingData_ShouldNotReceiveLargeData()
    {
        var data = new byte[1281];
        var destination = new IPEndPoint(IPAddress.Loopback, _receiverPort);
        var udpClient = new UdpClient(1236);
        await udpClient.SendAsync(data, destination);
        var ex = Assert.ThrowsAsync<InvalidPacketException>(() => _receiver.ReceiveAsync());
        Assert.AreEqual("Packet is too large", ex!.Message);
    }

    [Test]
    public async Task Test_ReceivingData_ShouldNotReceiveSmallData()
    {
        var data = new byte[62];
        var destination = new IPEndPoint(IPAddress.Loopback, _receiverPort);
        var udpClient = new UdpClient(1237);
        await udpClient.SendAsync(data, destination);
        var ex = Assert.ThrowsAsync<InvalidPacketException>(() => _receiver.ReceiveAsync());
        Assert.AreEqual("Packet is too small", ex!.Message);
    }

    [Test]
    public async Task Test_ReceivingData_ShouldTimeout()
    {
        var data = new byte[100];
        var destination = new IPEndPoint(IPAddress.Loopback, 1236);
        await _sender.SendAsync(data, destination);
        var ex = Assert.ThrowsAsync<UdpTimeoutException>(() => _receiver.ReceiveAsync());
        Assert.AreEqual("Receive timed out", ex!.Message);
    }
}