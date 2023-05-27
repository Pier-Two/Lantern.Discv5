using System.Net;
using System.Net.Sockets;
using System.Text;
using Lantern.Discv5.WireProtocol.Connection;
using Lantern.Discv5.WireProtocol.Logging.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using NUnit.Framework;

namespace Lantern.Discv5.WireProtocol.Tests;

[TestFixture]
public class UdpConnectionTests
{
    private UdpConnection _sender = null!;
    private UdpConnection _receiver = null!;
    private ConnectionOptions _senderOptions = null!;
    private ConnectionOptions _receiverOptions = null!;

    [SetUp]
    public void Setup()
    {
        // Use random function to generate ports
        _senderOptions = new ConnectionOptions.Builder()
            .WithPort(port: 1234)
            .Build();
        
        _receiverOptions = new ConnectionOptions.Builder()
            .WithPort(port: 1235)
            .Build();
        
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder
                .AddFilter("Microsoft", LogLevel.Warning)
                .AddFilter("System", LogLevel.Warning)
                .AddSimpleConsole(options =>
                {
                    options.ColorBehavior = LoggerColorBehavior.Enabled;
                    options.IncludeScopes = false;
                    options.SingleLine = true;
                    options.TimestampFormat = "HH:mm:ss ";
                    options.UseUtcTimestamp = true;
                });
        });

        _sender = new UdpConnection(_senderOptions, loggerFactory);
        _receiver = new UdpConnection(_receiverOptions, loggerFactory);
    }

    [TearDown]
    public void Cleanup()
    {
        _sender.Close();
        _sender.Dispose();
        _receiver.Close();
        _receiver.Dispose();
    }

    [Test]
    public async Task Test_SendingAndReceivingData_ShouldSendAndReceiveDataCorrectly()
    {
        const string enrString =
            "enr:-IS4QHCYrYZbAKWCBRlAy5zzaDZXJBGkcnh4MHcBFZntXNFrdvJjX04jRzjzCBOonrkTfj499SZuOh8R33Ls8RRcy5wBgmlkgnY0gmlwhH8AAAGJc2VjcDI1NmsxoQPKY0yuDUmstAHYpMa2_oxVtw0RW_QAdpzBQA8yWM0xOIN1ZHCCdl8";
        var data = Encoding.ASCII.GetBytes(enrString);
        var destination = new IPEndPoint(_receiverOptions.LocalIpAddress, _receiverOptions.Port);
        await _sender.SendAsync(data, destination);

        var receiveBytes = await _receiver.ReceiveAsync();
        var receiveString = Encoding.ASCII.GetString(receiveBytes.Buffer);

        Assert.AreEqual(enrString, receiveString);
    }

    [Test]
    public void Test_SendingData_ShouldNotSendLargeData()
    {
        var data = new byte[1281];
        var destination = new IPEndPoint(_receiverOptions.LocalIpAddress, _receiverOptions.Port);
        var ex = Assert.ThrowsAsync<InvalidPacketException>(() => _sender.SendAsync(data, destination));
        Assert.AreEqual("Packet is too large", ex!.Message);
    }

    [Test]
    public void Test_SendingData_ShouldNotSendSmallData()
    {
        var data = new byte[62];
        var destination = new IPEndPoint(_receiverOptions.LocalIpAddress, _receiverOptions.Port);
        var ex = Assert.ThrowsAsync<InvalidPacketException>(() => _sender.SendAsync(data, destination));
        Assert.AreEqual("Packet is too small", ex!.Message);
    }

    [Test]
    public async Task Test_ReceivingData_ShouldNotReceiveLargeData()
    {
        var data = new byte[1281];
        var destination = new IPEndPoint(_receiverOptions.LocalIpAddress, _receiverOptions.Port);
        var udpClient = new UdpClient(1236);
        await udpClient.SendAsync(data, destination);
        var ex = Assert.ThrowsAsync<InvalidPacketException>(() => _receiver.ReceiveAsync());
        Assert.AreEqual("Packet is too large", ex!.Message);
    }

    [Test]
    public async Task Test_ReceivingData_ShouldNotReceiveSmallData()
    {
        var data = new byte[62];
        var destination = new IPEndPoint(_receiverOptions.LocalIpAddress, _receiverOptions.Port);
        var udpClient = new UdpClient(1237);
        await udpClient.SendAsync(data, destination);
        var ex = Assert.ThrowsAsync<InvalidPacketException>(() => _receiver.ReceiveAsync());
        Assert.AreEqual("Packet is too small", ex!.Message);
    }

    [Test]
    public async Task Test_ReceivingData_ShouldTimeout()
    {
        var data = new byte[100];
        var destination = new IPEndPoint(_receiverOptions.LocalIpAddress, 1236);
        await _sender.SendAsync(data, destination);
        var ex = Assert.ThrowsAsync<UdpTimeoutException>(() => _receiver.ReceiveAsync());
        Assert.AreEqual("Receive timed out", ex!.Message);
    }
}