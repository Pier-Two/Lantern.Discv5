using System.Net;
using Lantern.Discv5.WireProtocol.Messages;
using Lantern.Discv5.WireProtocol.Table;
using NUnit.Framework;

namespace Lantern.Discv5.WireProtocol.Tests;

[TestFixture]
public class MessageTests
{
    [Test]
    public void Test_PingMessage_ShouldEncodeCorrectly()
    {
        var pingMessage = new Ping(12);
        var encodedMessage = pingMessage.EncodeMessage();
        var expectedPrefix = new[] { MessageTypes.Ping, 202, 136 };
        Assert.AreEqual(12, encodedMessage.Length);
        Assert.AreEqual(expectedPrefix, new ArraySegment<byte>(encodedMessage, 0, 3));
        Assert.AreEqual(12, encodedMessage[^1]);
    }

    [Test]
    public void Test_PingMessage_ShouldDecodeCorrectly()
    {
        var pingMessage = new Ping(12);
        var newPingMessage = Ping.DecodeMessage(pingMessage.EncodeMessage());
        Assert.AreEqual(pingMessage.RequestId, newPingMessage.RequestId);
        Assert.AreEqual(pingMessage.EnrSeq, newPingMessage.EnrSeq);
    }

    [Test]
    public void Test_PongMessage_ShouldEncodeCorrectly()
    {
        var recipientIp = IPAddress.Loopback;
        var pongMessage = new Pong(12, recipientIp, 3402);
        var encodedMessage = pongMessage.EncodeMessage();
        var expectedPrefix = new[] { 2, 210, 136 };
        var expectedSuffix = new[] { 12, 132, 127, 0, 0, 1, 130, 13, 74 };

        Assert.AreEqual(20, encodedMessage.Length);
        Assert.AreEqual(expectedPrefix, new ArraySegment<byte>(encodedMessage, 0, 3));
        Assert.AreEqual(expectedPrefix, new ArraySegment<byte>(encodedMessage, 0, 3));
        Assert.AreEqual(expectedSuffix, new ArraySegment<byte>(encodedMessage, 11, 9));
    }

    [Test]
    public void Test_PongMessage_ShouldDecodeCorrectly()
    {
        var recipientIp = IPAddress.Loopback;
        var pongMessage = new Pong(12, recipientIp, 3402);
        var newPongMessage = Pong.DecodeMessage(pongMessage.EncodeMessage());
        Assert.AreEqual(pongMessage.RequestId, newPongMessage.RequestId);
        Assert.AreEqual(pongMessage.EnrSeq, newPongMessage.EnrSeq);
        Assert.AreEqual(pongMessage.RecipientIp, newPongMessage.RecipientIp);
        Assert.AreEqual(pongMessage.RecipientPort, newPongMessage.RecipientPort);
    }

    [Test]
    public void Test_FindNode_ShouldEncodeCorrectly()
    {
        var firstNodeId = Convert.FromHexString("44b50f5e91964b67f544cec6d884bc27a83ae084fb2d0dae85c552722adde24c");
        var secondNodeId = Convert.FromHexString("922259344d3e88c6c34c94192f598dca417174209f9dbfd423038a6460c59bd6");
        var thirdNodeId = Convert.FromHexString("bd9261edff7e5908db711d9acd5470296af8a695646b3585255d8dc51a319e3c");
        var fourthNodeId = Convert.FromHexString("c4606371d7a8f19ff21404f7cb61c9d9f0a1440597717d6b0e5de92004f52ed9");
        var firstDistance = TableUtility.Log2Distance(firstNodeId, secondNodeId);
        var secondDistance = TableUtility.Log2Distance(thirdNodeId, fourthNodeId);
        var distances = new[] { firstDistance, secondDistance };
        var findNodeMessage = new FindNode(distances);
        var encodedMessage = findNodeMessage.EncodeMessage();
        var expectedPrefix = new[] { 3, 207, 136 };
        var expectedSuffix = new[] { 197, 130, 1, 0, 129, 255 };
        Assert.AreEqual(17, encodedMessage.Length);
        Assert.AreEqual(expectedPrefix, new ArraySegment<byte>(encodedMessage, 0, 3));
        Assert.AreEqual(expectedSuffix, new ArraySegment<byte>(encodedMessage, 11, 6));
    }

    [Test]
    public void Test_FindNode_ShouldDecodeCorrectly()
    {
        var firstNodeId = Convert.FromHexString("44b50f5e91964b67f544cec6d884bc27a83ae084fb2d0dae85c552722adde24c");
        var secondNodeId = Convert.FromHexString("922259344d3e88c6c34c94192f598dca417174209f9dbfd423038a6460c59bd6");
        var thirdNodeId = Convert.FromHexString("bd9261edff7e5908db711d9acd5470296af8a695646b3585255d8dc51a319e3c");
        var fourthNodeId = Convert.FromHexString("c4606371d7a8f19ff21404f7cb61c9d9f0a1440597717d6b0e5de92004f52ed9");
        var firstDistance = TableUtility.Log2Distance(firstNodeId, secondNodeId);
        var secondDistance = TableUtility.Log2Distance(thirdNodeId, fourthNodeId);
        var distances = new[] { firstDistance, secondDistance };
        var findNodeMessage = new FindNode(distances);
        var newFindNodeMessage = FindNode.DecodeMessage(findNodeMessage.EncodeMessage());
        Assert.AreEqual(findNodeMessage.Distances, newFindNodeMessage.Distances);
    }

    [Test]
    public void Test_Nodes_ShouldEncodeCorrectly()
    {
        var firstEnrString = "enr:-IS4QHCYrYZbAKWCBRlAy5zzaDZXJBGkcnh4MHcBFZntXNFrdvJjX04jRzjzCBOonrkTfj499SZuOh8R33Ls8RRcy5wBgmlkgnY0gmlwhH8AAAGJc2VjcDI1NmsxoQPKY0yuDUmstAHYpMa2_oxVtw0RW_QAdpzBQA8yWM0xOIN1ZHCCdl8";
        var secondEnrString = "enr:-Ly4QOS00hvPDddEcCpwA1cMykWNdJUK50AjbRgbLZ9FLPyBa78i0NwsQZLSV67elpJU71L1Pt9yqVmE1C6XeSI-LV8Bh2F0dG5ldHOIAAAAAAAAAACEZXRoMpDuKNezAAAAckYFAAAAAAAAgmlkgnY0gmlwhEDhTgGJc2VjcDI1NmsxoQIgMUMFvJGlr8dI1TEQy-K78u2TJE2rWvah9nGqLQCEGohzeW5jbmV0cwCDdGNwgiMog3VkcIIjKA";
        var enrs = new[] { firstEnrString, secondEnrString };
        var nodesMessage = new Nodes(2, enrs);
        var encodedMessage = nodesMessage.EncodeMessage();
        var expectedPrefix = new[] { 4, 249, 1, 81, 136 };
        Assert.AreEqual(341, encodedMessage.Length);
        Assert.AreEqual(expectedPrefix, new ArraySegment<byte>(encodedMessage, 0, 5));
        
    }
    
    

}