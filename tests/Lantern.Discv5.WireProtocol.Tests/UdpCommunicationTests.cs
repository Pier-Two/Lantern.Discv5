using System.Net;
using System.Net.Sockets;
using System.Text;
using NUnit.Framework;

namespace Lantern.Discv5.WireProtocol.Tests;

[TestFixture]
public class UdpCommunicationTests
{
    [SetUp]
    public void Setup()
    {
        _senderPort = 1234;
        _receiverPort = 1235;
        _sender = new UdpCommunication(_senderPort);
        _receiver = new UdpCommunication(_receiverPort);
    }

    private UdpCommunication _sender = null!;
    private UdpCommunication _receiver = null!;
    private int _senderPort;
    private int _receiverPort;

    [Test]
    public async Task Test_SendingAndReceivingData_ShouldSendAndReceiveDataCorrectly()
    {
        const string enrString =
            "enr:-IS4QHCYrYZbAKWCBRlAy5zzaDZXJBGkcnh4MHcBFZntXNFrdvJjX04jRzjzCBOonrkTfj499SZuOh8R33Ls8RRcy5wBgmlkgnY0gmlwhH8AAAGJc2VjcDI1NmsxoQPKY0yuDUmstAHYpMa2_oxVtw0RW_QAdpzBQA8yWM0xOIN1ZHCCdl8";
        try
        {
            var data = Encoding.ASCII.GetBytes(enrString);
            var destination = new IPEndPoint(IPAddress.Loopback, _receiverPort);
            await _sender.SendAsync(data, destination);

            var receiveBytes = await _receiver.ReceiveAsync();
            var receiveString = Encoding.ASCII.GetString(receiveBytes);

            Assert.AreEqual(enrString, receiveString);
        }
        finally
        {
            _sender.Close();
            _receiver.Close();
        }
    }

    [Test]
    public void Test_SendingData_ShouldNotSendLargeData()
    {
        try
        {
            var data = new byte[1281];
            var destination = new IPEndPoint(IPAddress.Loopback, _receiverPort);
            var ex = Assert.ThrowsAsync<InvalidDataException>(() => _sender.SendAsync(data, destination));
            Assert.AreEqual("Packet is too large", ex!.Message);
        }
        finally
        {
            _sender.Close();
            _receiver.Close();
        }
    }

    [Test]
    public void Test_SendingData_ShouldNotSendSmallData()
    {
        try
        {
            var data = new byte[62];
            var destination = new IPEndPoint(IPAddress.Loopback, _receiverPort);
            var ex = Assert.ThrowsAsync<InvalidDataException>(() => _sender.SendAsync(data, destination));
            Assert.AreEqual("Packet is too small", ex!.Message);
        }
        finally
        {
            _sender.Close();
            _receiver.Close();
        }
    }

    [Test]
    public async Task Test_ReceivingData_ShouldNotReceiveLargeData()
    {
        try
        {
            var data = new byte[1281];
            var destination = new IPEndPoint(IPAddress.Loopback, _receiverPort);
            var udpClient = new UdpClient(1236);
            await udpClient.SendAsync(data, destination);
            var ex = Assert.ThrowsAsync<InvalidDataException>(() => _receiver.SendAsync(data, destination));
            Assert.AreEqual("Packet is too large", ex!.Message);
        }
        finally
        {
            _sender.Close();
            _receiver.Close();
        }
    }

    [Test]
    public async Task Test_ReceivingData_ShouldNotReceiveSmallData()
    {
        try
        {
            var data = new byte[62];
            var destination = new IPEndPoint(IPAddress.Loopback, _receiverPort);
            var udpClient = new UdpClient(1237);
            await udpClient.SendAsync(data, destination);
            var ex = Assert.ThrowsAsync<InvalidDataException>(() => _receiver.SendAsync(data, destination));
            Assert.AreEqual("Packet is too small", ex!.Message);
        }
        finally
        {
            _sender.Close();
            _receiver.Close();
        }
    }

    [Test]
    public async Task Test_ReceivingData_ShouldTimeout()
    {
        try
        {
            var data = new byte[100];
            var destination = new IPEndPoint(IPAddress.Loopback, 1236);
            await _sender.SendAsync(data, destination);
            var ex = Assert.ThrowsAsync<TimeoutException>(() => _receiver.ReceiveAsync());
            Assert.AreEqual("Receive timed out", ex!.Message);
        }
        finally
        {
            _sender.Close();
            _receiver.Close();
        }
    }
}