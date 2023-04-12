using Lantern.Discv5.Enr;
using Lantern.Discv5.Enr.EnrFactory;
using Lantern.Discv5.Enr.IdentityScheme;
using Lantern.Discv5.Enr.IdentityScheme.V4;
using Lantern.Discv5.Rlp;
using Lantern.Discv5.WireProtocol.Crypto;
using Lantern.Discv5.WireProtocol.Messages.Decoders;
using Lantern.Discv5.WireProtocol.Messages.Requests;
using Lantern.Discv5.WireProtocol.Packets;
using Lantern.Discv5.WireProtocol.Packets.Constants;
using Lantern.Discv5.WireProtocol.Packets.PacketHeaders;
using Lantern.Discv5.WireProtocol.Packets.Types;
using Lantern.Discv5.WireProtocol.Session;
using NBitcoin.Secp256k1;
using NUnit.Framework;

namespace Lantern.Discv5.WireProtocol.Tests;

[TestFixture]
public class PacketTests
{
    [Test]
    public void Test_OrdinaryPacket_ShouldGeneratePacketCorrectly()
    {
        var nodeAId = Convert.FromHexString("aaaa8419e9f49d0083561b48287df592939a8d19947d8c0ef88f2a4856a69fbb");
        var nodeBId = Convert.FromHexString("bbbb9d047f0488c0b5a93c1c3f2d8bafc7c8ff337024a55434a0d0555de64db9");
        var nonce = Convert.FromHexString("ffffffffffffffffffffffff");
        var ordinaryPacket = new OrdinaryPacket(nodeAId);
        var staticHeader = new StaticHeader(ProtocolConstants.ProtocolId, ProtocolConstants.Version,
            ordinaryPacket.AuthData, (byte)PacketType.Ordinary, nonce);
        var maskedIv = Convert.FromHexString("00000000000000000000000000000000");
        var maskedHeader = new MaskedHeader(nodeBId, maskedIv);
        var encryptedHeader = maskedHeader.GetMaskedHeader(staticHeader.GetHeader());
        var expectedEncryptedHeader = Convert.FromHexString("088b3d4342774649325f313964a39e55ea96c005ad52be8c7560413a7008f16c9e6d2f43bbea8814a546b7409ce783d34c4f53245d08da");
        Assert.IsTrue(encryptedHeader.SequenceEqual(expectedEncryptedHeader));
    }
    
    [Test]
    public void Test_OrdinaryPacket_ShouldDecryptPacketCorrectly()
    {
        var nodeBId = Convert.FromHexString("bbbb9d047f0488c0b5a93c1c3f2d8bafc7c8ff337024a55434a0d0555de64db9");
        var nonce = Convert.FromHexString("ffffffffffffffffffffffff");
        var ordinaryPacket = Convert.FromHexString("00000000000000000000000000000000088b3d4342774649325f313964a39e55ea96c005ad52be8c7560413a7008f16c9e6d2f43bbea8814a546b7409ce783d34c4f53245d08dab84102ed931f66d1492acb308fa1c6715b9d139b81acbdcc");
        var decryptedData = AesUtils.AesCtrDecrypt(nodeBId[..16], ordinaryPacket[..16],ordinaryPacket[16..]);
        var staticHeader = StaticHeader.DecodeFromBytes(decryptedData);
        
        Assert.AreEqual(ProtocolConstants.ProtocolId, staticHeader.ProtocolId);
        Assert.AreEqual(ProtocolConstants.Version, staticHeader.Version);
        Assert.AreEqual((byte)PacketType.Ordinary, staticHeader.Flag);
        Assert.IsTrue(nonce.SequenceEqual(staticHeader.Nonce));
        Assert.AreEqual(AuthDataSizes.Ordinary, staticHeader.AuthData.Length);
    }
    
    [Test]
    public void Test_WhoAreYouPacket_ShouldGenerateCorrectly()
    {
        var nodeBId = Convert.FromHexString("bbbb9d047f0488c0b5a93c1c3f2d8bafc7c8ff337024a55434a0d0555de64db9");
        var nonce = Convert.FromHexString("0102030405060708090a0b0c");
        var idNonce = Convert.FromHexString("0102030405060708090a0b0c0d0e0f10");
        var maskedIv = Convert.FromHexString("00000000000000000000000000000000");
        var whoAreYouPacket = new WhoAreYouPacket(idNonce, 0);
        var staticHeader = new StaticHeader(ProtocolConstants.ProtocolId, ProtocolConstants.Version,
            whoAreYouPacket.AuthData, (byte)PacketType.WhoAreYou, nonce);
        var maskedHeader = new MaskedHeader(nodeBId, maskedIv);
        var packet = ByteArrayUtils.JoinByteArrays(maskedIv, maskedHeader.GetMaskedHeader(staticHeader.GetHeader()));
        var expectedWhoAreYouPacket =
            Convert.FromHexString(
                "00000000000000000000000000000000088b3d434277464933a1ccc59f5967ad1d6035f15e528627dde75cd68292f9e6c27d6b66c8100a873fcbaed4e16b8d");
        Assert.IsTrue(packet.SequenceEqual(expectedWhoAreYouPacket));
    }

    [Test]
    public void Test_WhoAreYouPacket_ShouldDecryptPacketCorrectly()
    {
        var nodeBId = Convert.FromHexString("bbbb9d047f0488c0b5a93c1c3f2d8bafc7c8ff337024a55434a0d0555de64db9");
        var whoAreYouPacket =
            Convert.FromHexString(
                "00000000000000000000000000000000088b3d434277464933a1ccc59f5967ad1d6035f15e528627dde75cd68292f9e6c27d6b66c8100a873fcbaed4e16b8d");
        var decryptedData = AesUtils.AesCtrDecrypt(nodeBId[..16], whoAreYouPacket[..16],whoAreYouPacket[16..]);
        var staticHeader = StaticHeader.DecodeFromBytes(decryptedData);
        var challengeData = ByteArrayUtils.Concatenate(whoAreYouPacket[..16], staticHeader.GetHeader());
        var whoAreYou = WhoAreYouPacket.DecodeAuthData(staticHeader.AuthData);
        var expectedChallengeData =
            Convert.FromHexString(
                "000000000000000000000000000000006469736376350001010102030405060708090a0b0c00180102030405060708090a0b0c0d0e0f100000000000000000");
        var expectedNone = Convert.FromHexString("0102030405060708090a0b0c");
        var expectedId = Convert.FromHexString("0102030405060708090a0b0c0d0e0f10");
  
        Assert.IsTrue(challengeData.SequenceEqual(expectedChallengeData));
        Assert.IsTrue(staticHeader.Nonce.SequenceEqual(expectedNone));
        Assert.IsTrue(whoAreYou.IdNonce.SequenceEqual(expectedId));
        Assert.AreEqual(0, whoAreYou.EnrSeq);
    }
    
    [Test]
    public void Test_HandshakePacket_ShouldGenerateCorrectly()
    {
        var nodeAId = Convert.FromHexString("aaaa8419e9f49d0083561b48287df592939a8d19947d8c0ef88f2a4856a69fbb");
        var nodeBId = Convert.FromHexString("bbbb9d047f0488c0b5a93c1c3f2d8bafc7c8ff337024a55434a0d0555de64db9");
        var nodeAPrivKey = Convert.FromHexString("eef77acb6c6a6eebc5b363a475ac583ec7eccdb42b6481424c60f59aa326547f");
        var nodeAEphemeralPrivKey = Convert.FromHexString("0288ef00023598499cb6c940146d050d2b1fb914198c327f76aad590bead68b6");
        var nodeACrypto = new CryptoSession(new TestSessionKeys(nodeAPrivKey, nodeAEphemeralPrivKey));
        var nodeAEphemeralPubkey = nodeACrypto.EphemeralPublicKey;
        var nodeBPubkey =
            new CryptoSession(new TestSessionKeys(Convert.FromHexString("66fb62bfbd66b9177a138c1e5cddbe4f7c30c343e94e68df8769459cb1cde628"))).PublicKey;
        var nonce = Convert.FromHexString("ffffffffffffffffffffffff");
        var maskedIv = Convert.FromHexString("00000000000000000000000000000000");
        var challengeData = Convert.FromHexString("000000000000000000000000000000006469736376350001010102030405060708090a0b0c00180102030405060708090a0b0c0d0e0f100000000000000001");
        var idSignature = nodeACrypto.GenerateIdSignature(challengeData, nodeAEphemeralPubkey, nodeBId);
        var sharedSecret = SessionUtils.GenerateSharedSecret(nodeBPubkey, nodeAEphemeralPrivKey, Context.Instance);
        var sessionKeys = SessionUtils.GenerateKeyDataFromSecret(sharedSecret, nodeAId, nodeBId, challengeData);
        var handshakePacket = new HandshakePacket(idSignature, nodeAEphemeralPubkey, nodeAId);
        var staticHeader = new StaticHeader(ProtocolConstants.ProtocolId, ProtocolConstants.Version, handshakePacket.AuthData, (byte)PacketType.Handshake, nonce);
        var maskedHeader = new MaskedHeader(nodeBId, maskedIv);
        var pingMessage = new PingMessage(1)
        {
            RequestId = new byte[] { 0, 0, 0, 1 }
        };
        var messagePt = pingMessage.EncodeMessage();
        var messageAd = ByteArrayUtils.JoinByteArrays(maskedIv, staticHeader.GetHeader());
        var encryptedMessage = AesUtils.AesGcmEncrypt(sessionKeys.InitiatorKey, nonce, messagePt, messageAd);
        var packet = ByteArrayUtils.Concatenate(maskedIv, maskedHeader.GetMaskedHeader(staticHeader.GetHeader()), encryptedMessage);
        var expectedPacket = Convert.FromHexString("00000000000000000000000000000000088b3d4342774649305f313964a39e55ea96c005ad521d8c7560413a7008f16c9e6d2f43bbea8814a546b7409ce783d34c4f53245d08da4bb252012b2cba3f4f374a90a75cff91f142fa9be3e0a5f3ef268ccb9065aeecfd67a999e7fdc137e062b2ec4a0eb92947f0d9a74bfbf44dfba776b21301f8b65efd5796706adff216ab862a9186875f9494150c4ae06fa4d1f0396c93f215fa4ef524f1eadf5f0f4126b79336671cbcf7a885b1f8bd2a5d839cf8");
        Assert.IsTrue(packet.SequenceEqual(expectedPacket));
    }
    
    [Test]
    public void Test_HandshakePingMessagePacket_ShouldDecryptCorrectly()
    {
        var nodeAPubkey = new CryptoSession(new TestSessionKeys(Convert.FromHexString("eef77acb6c6a6eebc5b363a475ac583ec7eccdb42b6481424c60f59aa326547f"))).PublicKey;
        var nodeBId = Convert.FromHexString("bbbb9d047f0488c0b5a93c1c3f2d8bafc7c8ff337024a55434a0d0555de64db9");
        var nodeBCrypto = new CryptoSession(new TestSessionKeys(Convert.FromHexString("66fb62bfbd66b9177a138c1e5cddbe4f7c30c343e94e68df8769459cb1cde628")));
        var packet = Convert.FromHexString(
            "00000000000000000000000000000000088b3d4342774649305f313964a39e55ea96c005ad521d8c7560413a7008f16c9e6d2f43bbea8814a546b7409ce783d34c4f53245d08da4bb252012b2cba3f4f374a90a75cff91f142fa9be3e0a5f3ef268ccb9065aeecfd67a999e7fdc137e062b2ec4a0eb92947f0d9a74bfbf44dfba776b21301f8b65efd5796706adff216ab862a9186875f9494150c4ae06fa4d1f0396c93f215fa4ef524f1eadf5f0f4126b79336671cbcf7a885b1f8bd2a5d839cf8");
        var decryptedData = AesUtils.AesCtrDecrypt(nodeBId[..16], packet[..16], packet[16..]);
        var staticHeader = StaticHeader.DecodeFromBytes(decryptedData);
        var handshakePacket = HandshakePacket.DecodeAuthData(staticHeader.AuthData);
        var idSignature = handshakePacket.IdSignature;
        var maskingIv = packet[..16];
        var challengeData =
            Convert.FromHexString(
                "000000000000000000000000000000006469736376350001010102030405060708090a0b0c00180102030405060708090a0b0c0d0e0f100000000000000001");
        var ephemeralPubKey = handshakePacket.EphPubkey;
        var result = SessionUtils.VerifyIdSignature(idSignature, nodeAPubkey, challengeData, ephemeralPubKey, nodeBId, Context.Instance);
        var sharedSecret = nodeBCrypto.GenerateSharedSecret(ephemeralPubKey);
        var sessionKeys = SessionUtils.GenerateKeyDataFromSecret(sharedSecret, handshakePacket.SrcId!, nodeBId, challengeData);
        var messageAd = ByteArrayUtils.JoinByteArrays(maskingIv, staticHeader.GetHeader());
        var encryptedMessage = packet[^staticHeader.EncryptedMessageLength..]; // This indexer statement extracts the encrypted message from the packet
        var decryptedMessage = AesUtils.AesGcmDecrypt(sessionKeys.InitiatorKey, staticHeader.Nonce,
            encryptedMessage, messageAd);
        var pingMessage = new PingDecoder().DecodeMessage(decryptedMessage);
        var expectedRequestId = Convert.FromHexString("00000001");
        var expectedEnrSeq = 1;
        
        Assert.IsTrue(result);
        Assert.AreEqual(expectedRequestId, pingMessage.RequestId);
        Assert.AreEqual(expectedEnrSeq, pingMessage.EnrSeq);
    }
    
    [Test]
    public void Test_HandshakePingMessagePacketWithEnr_ShouldDecryptCorrectly()
    {
        var nodeAPubkey = new CryptoSession(new TestSessionKeys(Convert.FromHexString("eef77acb6c6a6eebc5b363a475ac583ec7eccdb42b6481424c60f59aa326547f"))).PublicKey;
        var nodeBId = Convert.FromHexString("bbbb9d047f0488c0b5a93c1c3f2d8bafc7c8ff337024a55434a0d0555de64db9");
        var nodeBCrypto = new CryptoSession(new TestSessionKeys(Convert.FromHexString("66fb62bfbd66b9177a138c1e5cddbe4f7c30c343e94e68df8769459cb1cde628")));
        var packet = Convert.FromHexString(
            "00000000000000000000000000000000088b3d4342774649305f313964a39e55ea96c005ad539c8c7560413a7008f16c9e6d2f43bbea8814a546b7409ce783d34c4f53245d08da4bb23698868350aaad22e3ab8dd034f548a1c43cd246be98562fafa0a1fa86d8e7a3b95ae78cc2b988ded6a5b59eb83ad58097252188b902b21481e30e5e285f19735796706adff216ab862a9186875f9494150c4ae06fa4d1f0396c93f215fa4ef524e0ed04c3c21e39b1868e1ca8105e585ec17315e755e6cfc4dd6cb7fd8e1a1f55e49b4b5eb024221482105346f3c82b15fdaae36a3bb12a494683b4a3c7f2ae41306252fed84785e2bbff3b022812d0882f06978df84a80d443972213342d04b9048fc3b1d5fcb1df0f822152eced6da4d3f6df27e70e4539717307a0208cd208d65093ccab5aa596a34d7511401987662d8cf62b139471");
        var decryptedData = AesUtils.AesCtrDecrypt(nodeBId[..16], packet[..16],packet[16..]);
        var staticHeader = StaticHeader.DecodeFromBytes(decryptedData);
        var handshakePacket = HandshakePacket.DecodeAuthData(staticHeader.AuthData);
        var identityVerifier = new IdentitySchemeV4Verifier();
        
        // If it contains a record, it verifies the record's signature 
        // Then, it verifies the 'id-signature' by extracting ephemeral public key from the handshake packet and using nodeB challenge data
        var enr = new EnrRecordFactory().CreateFromBytes(handshakePacket.Record!);
        var enrRecordSignatureVerify = identityVerifier.VerifyRecord(enr);
        var idSignature = handshakePacket.IdSignature;
        var maskingIv = packet[..16];
        var challengeData =
            Convert.FromHexString(
                "000000000000000000000000000000006469736376350001010102030405060708090a0b0c00180102030405060708090a0b0c0d0e0f100000000000000000");
        var ephemeralPubKey = handshakePacket.EphPubkey;
        var idSignatureVerify = SessionUtils.VerifyIdSignature(idSignature, nodeAPubkey, challengeData, ephemeralPubKey, nodeBId, Context.Instance);
        var sharedSecret = nodeBCrypto.GenerateSharedSecret(ephemeralPubKey);
        var sessionKeys = SessionUtils.GenerateKeyDataFromSecret(sharedSecret, handshakePacket.SrcId!, nodeBId, challengeData);
        var messageAd = ByteArrayUtils.JoinByteArrays(maskingIv, staticHeader.GetHeader());
        var encryptedMessage = packet[^staticHeader.EncryptedMessageLength..]; // This indexer statement extracts the encrypted message from the packet
        var decryptedMessage = AesUtils.AesGcmDecrypt(sessionKeys.InitiatorKey, staticHeader.Nonce,
            encryptedMessage, messageAd);
        var pingMessage = new PingDecoder().DecodeMessage(decryptedMessage);
        var expectedRequestId = Convert.FromHexString("00000001");
        var expectedEnrSeq = 1;
        
        Assert.IsTrue(enrRecordSignatureVerify);
        Assert.IsTrue(idSignatureVerify);
        Assert.AreEqual(expectedRequestId, pingMessage.RequestId);
        Assert.AreEqual(expectedEnrSeq, pingMessage.EnrSeq);
    }
}