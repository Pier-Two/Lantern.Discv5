using System.Net;
using Lantern.Discv5.WireProtocol.Connection;
using NUnit.Framework;

namespace Lantern.Discv5.WireProtocol.Tests;

public class ConnectionOptionsTests
{
    private ConnectionOptions _connectionOptions = null!;
    
    [Test]
    public void Test_ConnectionOptions_CreateDefault()
    {
        _connectionOptions = ConnectionOptions.Default;

        Assert.NotNull(_connectionOptions);
        Assert.AreEqual(9000, _connectionOptions.Port);
        Assert.AreEqual(1000, _connectionOptions.ReceiveTimeoutMs);
        Assert.AreEqual(2000, _connectionOptions.RequestTimeoutMs);
        Assert.AreEqual(500, _connectionOptions.CheckPendingRequestsDelayMs);
        Assert.AreEqual(1000, _connectionOptions.RemoveCompletedRequestsDelayMs);
    }
    
    [Test]
    public void Test_ConnectionOptions_Builder()
    {
        var ipAddress = IPAddress.Any;
        var port = 9000;
        var receiveTimeoutMs = 1000;
        var requestTimeoutMs = 2000;
        var checkPendingRequestsDelayMs = 500;
        var removeCompletedRequestsDelayMs = 1000;
        
        _connectionOptions = new ConnectionOptions.Builder()
            .WithIpAddress(ipAddress)
            .WithPort(port)
            .WithReqRespTimeoutMs(receiveTimeoutMs)
            .WithPendingRequestTimeoutMs(requestTimeoutMs)
            .WithCheckPendingRequestsDelayMs(checkPendingRequestsDelayMs)
            .WithRemoveFulfilledRequestsDelayMs(removeCompletedRequestsDelayMs)
            .Build();
        
        Assert.NotNull(_connectionOptions);
        Assert.AreEqual(ipAddress, _connectionOptions.IpAddress);
        Assert.AreEqual(port, _connectionOptions.Port);
        Assert.AreEqual(receiveTimeoutMs, _connectionOptions.ReceiveTimeoutMs);
        Assert.AreEqual(requestTimeoutMs, _connectionOptions.RequestTimeoutMs);
        Assert.AreEqual(checkPendingRequestsDelayMs, _connectionOptions.CheckPendingRequestsDelayMs);
        Assert.AreEqual(removeCompletedRequestsDelayMs, _connectionOptions.RemoveCompletedRequestsDelayMs);
    }
}