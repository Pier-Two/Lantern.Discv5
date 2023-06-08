using System.Net;
using Lantern.Discv5.Enr;
using Lantern.Discv5.Enr.EnrContent;
using Lantern.Discv5.Enr.EnrContent.Entries;
using Lantern.Discv5.Enr.EnrFactory;
using Lantern.Discv5.Enr.IdentityScheme.V4;
using Lantern.Discv5.WireProtocol.Connection;
using Lantern.Discv5.WireProtocol.Identity;
using Lantern.Discv5.WireProtocol.Logging;
using Lantern.Discv5.WireProtocol.Session;
using Lantern.Discv5.WireProtocol.Table;
using Lantern.Discv5.WireProtocol.Utility;
using NUnit.Framework;

namespace Lantern.Discv5.WireProtocol.Tests;

[TestFixture]
public class RoutingTableTests
{
    private static RoutingTable _routingTable = null!;
    
    [SetUp]
    public void Setup()
    {
        var connectionOptions = ConnectionOptions.Default;
        var sessionOptions = SessionOptions.Default;
        var tableOptions = TableOptions.Default;
        var loggerFactory = LoggingOptions.Default;
        var identityManager = new IdentityManager(connectionOptions, sessionOptions, loggerFactory);
        _routingTable = new RoutingTable(identityManager, loggerFactory, tableOptions);
    }

    [Test]
    public void Test()
    {
        var enrs = GenerateRandomEnrs(32);
        
        foreach (var enr in enrs)
        {
            _routingTable.UpdateFromEnr(enr);
        }

    }

    private static EnrRecord[] GenerateRandomEnrs(int count)
    {
        var enrs = new EnrRecord[count];
        
        for(var i = 0; i < count; i++)
        {
            var signer = new IdentitySchemeV4Signer(RandomUtility.GeneratePrivateKey(32));
            var ipAddress = new IPAddress(RandomUtility.GenerateRandomData(4));
            
            enrs[i] = new EnrBuilder()
                .WithSigner(signer)
                .WithEntry(EnrContentKey.Id, new EntryId("v4"))
                .WithEntry(EnrContentKey.Ip, new EntryIp(ipAddress))
                .WithEntry(EnrContentKey.Udp, new EntryUdp(Random.Shared.Next(0, 9000)))
                .WithEntry(EnrContentKey.Secp256K1, new EntrySecp256K1(signer.PublicKey))
                .Build();
        }

        return enrs;
    }
    
}