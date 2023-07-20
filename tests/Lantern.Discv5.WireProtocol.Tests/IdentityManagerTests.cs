using System.Net;
using Lantern.Discv5.Enr.EnrContent;
using Lantern.Discv5.Enr.EnrContent.Entries;
using Lantern.Discv5.WireProtocol.Connection;
using Lantern.Discv5.WireProtocol.Identity;
using Lantern.Discv5.WireProtocol.Logging;
using Lantern.Discv5.WireProtocol.Session;
using NUnit.Framework;

namespace Lantern.Discv5.WireProtocol.Tests;

[TestFixture]
public class IdentityManagerTests
{
    private static IdentityManager _identityManager = null!;
    
    [SetUp]
    public void Setup()
    {
        var connectionOptions = ConnectionOptions.Default;
        var sessionOptions = SessionOptions.Default;
        var loggerFactory = LoggingOptions.Default;
        _identityManager = new IdentityManager(connectionOptions, sessionOptions, loggerFactory);
    }
    
    [Test]
    public void Test_IdentityManager_ShouldResultInFalseWhenNoIpAndPortIsSet()
    {
        Assert.IsFalse(_identityManager.IsIpAddressAndPortSet());
    }
    
    [Test]
    public void Test_IdentityManager_ShouldUpdateIpAndPortCorrectly()
    {
        var node = _identityManager.Record;
        
        Assert.IsFalse(node.HasKey(EnrContentKey.Ip));
        Assert.IsFalse(node.HasKey(EnrContentKey.Udp));
        var endpoint = new IPEndPoint(ConnectionUtility.GetLocalIpAddress(), 1234);

        _identityManager.UpdateIpAddressAndPort(endpoint);
        
        Assert.AreEqual(endpoint.Address, node.GetEntry<EntryIp>(EnrContentKey.Ip).Value);
        Assert.AreEqual(endpoint.Port, node.GetEntry<EntryUdp>(EnrContentKey.Udp).Value);
    }
}