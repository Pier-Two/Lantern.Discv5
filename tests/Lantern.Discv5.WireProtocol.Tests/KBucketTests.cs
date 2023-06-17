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
public class KBucketTests
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
    public void Test_KBucket_AddingNodesToNonFullBucket()
    {
        var nodes = GenerateRandomNodeEntries(10);
        var bucket = new KBucket(LoggingOptions.Default);

        for (var i = 0; i < 10; i++)
        {
            bucket.Update(nodes[i]);
        }

        Assert.AreEqual(10, bucket.Nodes.Count());
        Assert.AreEqual(0, bucket.ReplacementCache.Count());
    }

    [Test]
    public void Test_RoutingTable_ShouldStoreNodesCorrectly()
    {
        var nodes = GenerateRandomNodeEntries(20);
        var bucket = new KBucket(LoggingOptions.Default);
        
        for (var i = 0; i < 16; i++)
        {
            bucket.Update(nodes[i]);
        }
        
        for (var i = 16; i < 20; i++)
        {
            bucket.Update(nodes[i]);
        }
        
        Assert.AreEqual(16, bucket.Nodes.Count());
        Assert.AreEqual(1, bucket.ReplacementCache.Count());
    }
    
    [Test]
    public void Test_KBucket_ShouldReplaceDeadNodesCorrectly()
    {
        var nodes = GenerateRandomNodeEntries(20);
        var bucket = new KBucket(LoggingOptions.Default);
        
        for (var i = 0; i < 16; i++)
        {
            bucket.Update(nodes[i]);
        }
        
        for (var i = 16; i < 20; i++)
        {
            bucket.AddToReplacementCache(nodes[i]);
        }
        
        nodes[0].IsLive = false;
        bucket.ReplaceDeadNode(nodes[0]);
        
        Assert.IsFalse(bucket.Nodes.Contains(nodes[0]));
        Assert.AreEqual(16, bucket.Nodes.Count());
        Assert.AreEqual(3, bucket.ReplacementCache.Count());
        Assert.IsTrue(bucket.Nodes.Contains(nodes[16]));
    }
    
    [Test]
    public void Test_KBucket_LeastRecentlySeenNodeRevalidation()
    {
        var nodes = GenerateRandomNodeEntries(20);
        var bucket = new KBucket(LoggingOptions.Default);

        for (var i = 0; i < 16; i++)
        {
            bucket.Update(nodes[i]);
        }

        nodes[17].IsLive = false; 
        bucket.Update(nodes[17]);

        Assert.AreEqual(16, bucket.Nodes.Count());
        Assert.AreEqual(1, bucket.ReplacementCache.Count());
        Assert.IsFalse(bucket.Nodes.Contains(nodes[17]));
    }

    private static NodeTableEntry[] GenerateRandomNodeEntries(int count)
    {
        var enrs = GenerateRandomEnrs(count);
        var nodeEntries = new NodeTableEntry[count];
        
        for(var i = 0; i < count; i++)
        {
            nodeEntries[i] = new NodeTableEntry(enrs[i], _identityManager.Verifier);
        }

        return nodeEntries;
    }
    
    private static EnrRecord[] GenerateRandomEnrs(int count)
    {
        var enrs = new EnrRecord[count];
        
        for(var i = 0; i < count; i++)
        {
            var signer = new IdentitySchemeV4Signer(RandomUtility.GenerateRandomData(32));
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