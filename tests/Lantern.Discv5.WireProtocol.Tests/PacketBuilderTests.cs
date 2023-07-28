using Lantern.Discv5.Enr.EnrFactory;
using Lantern.Discv5.Enr.IdentityScheme.Interfaces;
using Lantern.Discv5.Enr.IdentityScheme.V4;
using Lantern.Discv5.WireProtocol.Identity;
using Lantern.Discv5.WireProtocol.Message;
using Lantern.Discv5.WireProtocol.Packet;
using Lantern.Discv5.WireProtocol.Packet.Headers;
using Lantern.Discv5.WireProtocol.Session;
using Lantern.Discv5.WireProtocol.Utility;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace Lantern.Discv5.WireProtocol.Tests;

[TestFixture]
public class PacketBuilderTests
{
    private Mock<IIdentityManager> _identityManagerMock;
    private Mock<IRequestManager> _requestManagerMock;
    private Mock<ILogger<PacketBuilderTests>> _loggerMock;
    private Mock<ILoggerFactory> _loggerFactoryMock;
    private IPacketBuilder _packetBuilder;
    private IPacketProcessor _packetProcessor;
    private IIdentitySchemeVerifier _identitySchemeVerifier;

    [SetUp]
    public void SetUp()
    {
        _identityManagerMock = new Mock<IIdentityManager>();
        _requestManagerMock = new Mock<IRequestManager>();
        _loggerMock = new Mock<ILogger<PacketBuilderTests>>();
        _loggerFactoryMock = new Mock<ILoggerFactory>();
        _loggerFactoryMock
            .Setup(x => x.CreateLogger(It.IsAny<string>()))
            .Returns(_loggerMock.Object);
        
        _identitySchemeVerifier = new IdentitySchemeV4Verifier();
    }
    
    [Test]
    public void BuildRandomOrdinaryPacket_Should_Return_Valid_Packet()
    {
        var enrRecord = new EnrRecordFactory().CreateFromString("enr:-IS4QHCYrYZbAKWCBRlAy5zzaDZXJBGkcnh4MHcBFZntXNFrdvJjX04jRzjzCBOonrkTfj499SZuOh8R33Ls8RRcy5wBgmlkgnY0gmlwhH8AAAGJc2VjcDI1NmsxoQPKY0yuDUmstAHYpMa2_oxVtw0RW_QAdpzBQA8yWM0xOIN1ZHCCdl8");
        var nodeId = _identitySchemeVerifier.GetNodeIdFromRecord(enrRecord);
        
        _identityManagerMock
            .SetupGet(x => x.NodeId)
            .Returns(nodeId);

        _packetBuilder = new PacketBuilder(_identityManagerMock.Object, new AesCrypto(), _requestManagerMock.Object, _loggerFactoryMock.Object);
        _packetProcessor = new PacketProcessor(_identityManagerMock.Object, new AesCrypto());
        
        var result = _packetBuilder.BuildRandomOrdinaryPacket(nodeId);
        var staticHeader = _packetProcessor.GetStaticHeader(result.Item1);
        var maskingIv = _packetProcessor.GetMaskingIv(result.Item1);
        
        Assert.IsNotNull(result);
        Assert.IsInstanceOf<Tuple<byte[], StaticHeader>>(result);
        Assert.IsTrue(result.Item2.Flag == staticHeader.Flag);
        Assert.IsTrue(result.Item2.AuthData.SequenceEqual(staticHeader.AuthData));
        Assert.IsTrue(result.Item2.Version.SequenceEqual(staticHeader.Version));
        Assert.IsTrue(result.Item2.ProtocolId.SequenceEqual(staticHeader.ProtocolId));
        Assert.IsTrue(result.Item2.Nonce.SequenceEqual(staticHeader.Nonce));
        Assert.IsTrue(result.Item2.AuthDataSize == staticHeader.AuthDataSize);
        Assert.IsTrue(maskingIv.SequenceEqual(result.Item1[..16]));
        
        Assert.IsTrue(result.Item2.Nonce.Length == PacketConstants.NonceSize);
        Assert.IsTrue(result.Item2.AuthData.Length == PacketConstants.Ordinary);
        Assert.IsTrue(result.Item2.AuthDataSize == PacketConstants.Ordinary);
        Assert.IsTrue(result.Item2.Version.Length == PacketConstants.VersionSize);
        Assert.IsTrue(result.Item2.ProtocolId.Length == PacketConstants.ProtocolIdSize);
        Assert.IsTrue(maskingIv.Length == PacketConstants.MaskingIvSize);
    }
    
    [Test]
    public void BuildWhoAreYouPacketWithoutEnr_Should_Return_Valid_Packet()
    {
        var destNodeId = Convert.FromHexString("F92B82F11AF5ED0959135CDE8E64B626CAC4F16D05E43087224DEED25D1DBD72");
        var packetNonce = RandomUtility.GenerateRandomData(12);
        var maskingIv = Convert.FromHexString("EE1A7C1BB363686AACDAF6E84C66EB7A");

        _identityManagerMock
            .SetupGet(x => x.NodeId)
            .Returns(destNodeId);

        _packetBuilder = new PacketBuilder(_identityManagerMock.Object, new AesCrypto(), _requestManagerMock.Object, _loggerFactoryMock.Object);
        _packetProcessor = new PacketProcessor(_identityManagerMock.Object, new AesCrypto());

        var result = _packetBuilder.BuildWhoAreYouPacketWithoutEnr(destNodeId, packetNonce, maskingIv);
        var staticHeader = _packetProcessor.GetStaticHeader(result.Item1);
        var maskingIvResult = _packetProcessor.GetMaskingIv(result.Item1);
        
        Assert.IsNotNull(result);
        Assert.IsInstanceOf<Tuple<byte[], StaticHeader>>(result);
        Assert.IsTrue(result.Item2.Flag == staticHeader.Flag);
        Assert.IsTrue(result.Item2.AuthData.SequenceEqual(staticHeader.AuthData));
        Assert.IsTrue(result.Item2.Version.SequenceEqual(staticHeader.Version));
        Assert.IsTrue(result.Item2.ProtocolId.SequenceEqual(staticHeader.ProtocolId));
        Assert.IsTrue(result.Item2.Nonce.SequenceEqual(staticHeader.Nonce));
        Assert.IsTrue(maskingIvResult.SequenceEqual(result.Item1[..16]));
        
        Assert.IsTrue(result.Item2.AuthDataSize == staticHeader.AuthDataSize);
        Assert.IsTrue(result.Item2.Nonce.Length == PacketConstants.NonceSize);
        Assert.IsTrue(result.Item2.AuthData.Length == PacketConstants.WhoAreYou);
        Assert.IsTrue(result.Item2.AuthDataSize == PacketConstants.WhoAreYou);
        Assert.IsTrue(result.Item2.Version.Length == PacketConstants.VersionSize);
        Assert.IsTrue(result.Item2.ProtocolId.Length == PacketConstants.ProtocolIdSize);
        Assert.IsTrue(maskingIvResult.Length == PacketConstants.MaskingIvSize);
    }
    
    [Test]
    public void BuildWhoAreYouPacket_Should_Return_Valid_Packet()
    {
        var enrRecord = new EnrRecordFactory().CreateFromString("enr:-IS4QHCYrYZbAKWCBRlAy5zzaDZXJBGkcnh4MHcBFZntXNFrdvJjX04jRzjzCBOonrkTfj499SZuOh8R33Ls8RRcy5wBgmlkgnY0gmlwhH8AAAGJc2VjcDI1NmsxoQPKY0yuDUmstAHYpMa2_oxVtw0RW_QAdpzBQA8yWM0xOIN1ZHCCdl8");
        var destNodeId = Convert.FromHexString("F92B82F11AF5ED0959135CDE8E64B626CAC4F16D05E43087224DEED25D1DBD72");
        var packetNonce = RandomUtility.GenerateRandomData(12);
        var maskingIv = Convert.FromHexString("EE1A7C1BB363686AACDAF6E84C66EB7A");

        _identityManagerMock
            .SetupGet(x => x.NodeId)
            .Returns(destNodeId);

        _packetBuilder = new PacketBuilder(_identityManagerMock.Object, new AesCrypto(), _requestManagerMock.Object, _loggerFactoryMock.Object);
        _packetProcessor = new PacketProcessor(_identityManagerMock.Object, new AesCrypto());

        var result = _packetBuilder.BuildWhoAreYouPacket(destNodeId, packetNonce, enrRecord, maskingIv);
        var staticHeader = _packetProcessor.GetStaticHeader(result.Item1);
        var maskingIvResult = _packetProcessor.GetMaskingIv(result.Item1);
        
        Assert.IsNotNull(result);
        Assert.IsInstanceOf<Tuple<byte[], StaticHeader>>(result);
        Assert.IsTrue(result.Item2.Flag == staticHeader.Flag);
        Assert.IsTrue(result.Item2.AuthData.SequenceEqual(staticHeader.AuthData));
        Assert.IsTrue(result.Item2.Version.SequenceEqual(staticHeader.Version));
        Assert.IsTrue(result.Item2.ProtocolId.SequenceEqual(staticHeader.ProtocolId));
        Assert.IsTrue(result.Item2.Nonce.SequenceEqual(staticHeader.Nonce));
        Assert.IsTrue(result.Item2.AuthDataSize == staticHeader.AuthDataSize);
        Assert.IsTrue(maskingIvResult.SequenceEqual(result.Item1[..16]));
        
        Assert.IsTrue(result.Item2.Nonce.Length == PacketConstants.NonceSize);
        Assert.IsTrue(result.Item2.AuthData.Length == PacketConstants.WhoAreYou);
        Assert.IsTrue(result.Item2.AuthDataSize == PacketConstants.WhoAreYou);
        Assert.IsTrue(result.Item2.Version.Length == PacketConstants.VersionSize);
        Assert.IsTrue(result.Item2.ProtocolId.Length == PacketConstants.ProtocolIdSize);
        Assert.IsTrue(maskingIvResult.Length == PacketConstants.MaskingIvSize);
    }
    
    [Test]
    public void BuildHandshakePacket_Should_Return_Valid_Packet()
    {
        var enrRecord = new EnrRecordFactory().CreateFromString("enr:-IS4QHCYrYZbAKWCBRlAy5zzaDZXJBGkcnh4MHcBFZntXNFrdvJjX04jRzjzCBOonrkTfj499SZuOh8R33Ls8RRcy5wBgmlkgnY0gmlwhH8AAAGJc2VjcDI1NmsxoQPKY0yuDUmstAHYpMa2_oxVtw0RW_QAdpzBQA8yWM0xOIN1ZHCCdl8");
        var idSignature =
            Convert.FromHexString(
                "933468458F4F3DE637D9B84917DEDD8103C4A297A4980B703A727D017B92B2713A95FD12F0AD9264845FC6F1FF29F6E0706075019F43E8C624F4E58F78A6ED8C");
        var ephemeralPubKey =
            Convert.FromHexString("02044862F58DF5D9C43B6A4C4EBC9E9F4D0BFDFB42052265271B5A3A331803725C");
        var destNodeId = Convert.FromHexString("F92B82F11AF5ED0959135CDE8E64B626CAC4F16D05E43087224DEED25D1DBD72");
        var maskingIv = Convert.FromHexString("13E5792577482999855F3C5BE73FC550");
        var messageCount = Convert.FromHexString("00000000");

        _identityManagerMock
            .SetupGet(x => x.NodeId)
            .Returns(destNodeId);
        _identityManagerMock
            .SetupGet(x => x.Record)
            .Returns(enrRecord);

        _packetBuilder = new PacketBuilder(_identityManagerMock.Object, new AesCrypto(), _requestManagerMock.Object, _loggerFactoryMock.Object);
        _packetProcessor = new PacketProcessor(_identityManagerMock.Object, new AesCrypto());

        var result = _packetBuilder.BuildHandshakePacket(idSignature, ephemeralPubKey, destNodeId, maskingIv, messageCount);
        var staticHeader = _packetProcessor.GetStaticHeader(result.Item1);
        var maskingIvResult = _packetProcessor.GetMaskingIv(result.Item1);
        
        Assert.IsNotNull(result);
        Assert.IsInstanceOf<Tuple<byte[], StaticHeader>>(result);
        Assert.IsTrue(result.Item2.Flag == staticHeader.Flag);
        Assert.IsTrue(result.Item2.AuthData.SequenceEqual(staticHeader.AuthData));
        Assert.IsTrue(result.Item2.Version.SequenceEqual(staticHeader.Version));
        Assert.IsTrue(result.Item2.ProtocolId.SequenceEqual(staticHeader.ProtocolId));
        Assert.IsTrue(result.Item2.Nonce.SequenceEqual(staticHeader.Nonce));
        Assert.IsTrue(result.Item2.AuthDataSize == staticHeader.AuthDataSize);
        Assert.IsTrue(maskingIvResult.SequenceEqual(result.Item1[..16]));

        Assert.IsTrue(result.Item2.Nonce.Length == PacketConstants.NonceSize);
        Assert.IsTrue(result.Item2.AuthData.Length == PacketConstants.NodeIdSize + PacketConstants.SigSize + PacketConstants.EphemeralKeySize + idSignature.Length + ephemeralPubKey.Length + enrRecord.EncodeEnrRecord().Length);
        Assert.IsTrue(result.Item2.AuthDataSize == PacketConstants.NodeIdSize + PacketConstants.SigSize + PacketConstants.EphemeralKeySize + idSignature.Length + ephemeralPubKey.Length + enrRecord.EncodeEnrRecord().Length);
        Assert.IsTrue(result.Item2.Version.Length == PacketConstants.VersionSize);
        Assert.IsTrue(result.Item2.ProtocolId.Length == PacketConstants.ProtocolIdSize);
        Assert.IsTrue(maskingIvResult.Length == PacketConstants.MaskingIvSize);
    }
}