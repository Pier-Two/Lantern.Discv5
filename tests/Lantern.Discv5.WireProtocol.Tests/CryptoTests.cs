using Lantern.Discv5.Enr;
using Lantern.Discv5.WireProtocol.Packet;
using Lantern.Discv5.Rlp;
using Lantern.Discv5.WireProtocol.Messages;
using Lantern.Discv5.WireProtocol.Session;
using NUnit.Framework;

namespace Lantern.Discv5.WireProtocol.Tests;

[TestFixture]
public class CryptoTests
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
        var decryptedData = AesCryptography.AesCtrDecrypt(nodeBId[..16], ordinaryPacket);
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
        var decryptedData = AesCryptography.AesCtrDecrypt(nodeBId[..16], whoAreYouPacket);
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
        var nodeACrypto = new CryptoSession(nodeAPrivKey, nodeAEphemeralPrivKey);
        var nodeAEphemeralPubkey = nodeACrypto.GenerateEphemeralPublicKey();
        var nodeBPubkey = new CryptoSession(Convert.FromHexString("66fb62bfbd66b9177a138c1e5cddbe4f7c30c343e94e68df8769459cb1cde628")).GeneratePublicKey();
        var nonce = Convert.FromHexString("ffffffffffffffffffffffff");
        var maskedIv = Convert.FromHexString("00000000000000000000000000000000");
        var challengeData = Convert.FromHexString("000000000000000000000000000000006469736376350001010102030405060708090a0b0c00180102030405060708090a0b0c0d0e0f100000000000000001");
        var idSignature = nodeACrypto.GenerateIdSignature(challengeData, nodeAEphemeralPubkey, nodeBId);
        var sharedSecret = CryptoSession.GenerateSharedSecret(nodeBPubkey, nodeAEphemeralPrivKey);
        var sessionKeys = CryptoSession.GenerateKeyDataFromSecret(sharedSecret, nodeAId, nodeBId, challengeData);
        var handshakePacket = new HandshakePacket(idSignature, nodeAEphemeralPubkey, nodeAId);
        var staticHeader = new StaticHeader(ProtocolConstants.ProtocolId, ProtocolConstants.Version, handshakePacket.AuthData, (byte)PacketType.Handshake, nonce);
        var maskedHeader = new MaskedHeader(nodeBId, maskedIv);
        var pingMessage = new PingMessage(1)
        {
            RequestId = new byte[] { 0, 0, 0, 1 }
        };
        var messagePt = pingMessage.EncodeMessage();
        var messageAd = ByteArrayUtils.JoinByteArrays(maskedIv, staticHeader.GetHeader());
        var encryptedMessage = AesCryptography.AesGcmEncrypt(sessionKeys.InitiatorKey, nonce, messagePt, messageAd);
        var packet = ByteArrayUtils.Concatenate(maskedIv, maskedHeader.GetMaskedHeader(staticHeader.GetHeader()), encryptedMessage);
        var expectedPacket = Convert.FromHexString("00000000000000000000000000000000088b3d4342774649305f313964a39e55ea96c005ad521d8c7560413a7008f16c9e6d2f43bbea8814a546b7409ce783d34c4f53245d08da4bb252012b2cba3f4f374a90a75cff91f142fa9be3e0a5f3ef268ccb9065aeecfd67a999e7fdc137e062b2ec4a0eb92947f0d9a74bfbf44dfba776b21301f8b65efd5796706adff216ab862a9186875f9494150c4ae06fa4d1f0396c93f215fa4ef524f1eadf5f0f4126b79336671cbcf7a885b1f8bd2a5d839cf8");
        //Assert.IsTrue(packet.SequenceEqual(expectedPacket));
    }
    
    [Test]
    public void Test_HandshakePingMessagePacket_ShouldDecryptCorrectly()
    {
        var nodeAPubkey = new CryptoSession(Convert.FromHexString("eef77acb6c6a6eebc5b363a475ac583ec7eccdb42b6481424c60f59aa326547f")).GeneratePublicKey();
        var nodeBId = Convert.FromHexString("bbbb9d047f0488c0b5a93c1c3f2d8bafc7c8ff337024a55434a0d0555de64db9");
        var nodeBCrypto = new CryptoSession(Convert.FromHexString("66fb62bfbd66b9177a138c1e5cddbe4f7c30c343e94e68df8769459cb1cde628"));
        var packet = Convert.FromHexString(
            "00000000000000000000000000000000088b3d4342774649305f313964a39e55ea96c005ad521d8c7560413a7008f16c9e6d2f43bbea8814a546b7409ce783d34c4f53245d08da4bb252012b2cba3f4f374a90a75cff91f142fa9be3e0a5f3ef268ccb9065aeecfd67a999e7fdc137e062b2ec4a0eb92947f0d9a74bfbf44dfba776b21301f8b65efd5796706adff216ab862a9186875f9494150c4ae06fa4d1f0396c93f215fa4ef524f1eadf5f0f4126b79336671cbcf7a885b1f8bd2a5d839cf8");
        var decryptedData = AesCryptography.AesCtrDecrypt(nodeBId[..16], packet);
        var staticHeader = StaticHeader.DecodeFromBytes(decryptedData);
        var handshakePacket = HandshakePacket.DecodeAuthData(staticHeader.AuthData);
        var idSignature = handshakePacket.IdSignature;
        var maskingIv = packet[..16];
        var challengeData =
            Convert.FromHexString(
                "000000000000000000000000000000006469736376350001010102030405060708090a0b0c00180102030405060708090a0b0c0d0e0f100000000000000001");
        var ephemeralPubKey = handshakePacket.EphPubkey;
        var result = CryptoSession.VerifyIdSignature(idSignature, nodeAPubkey, challengeData, ephemeralPubKey, nodeBId);
        var sharedSecret = nodeBCrypto.GenerateSharedSecret(ephemeralPubKey);
        var sessionKeys = CryptoSession.GenerateKeyDataFromSecret(sharedSecret, handshakePacket.SrcId!, nodeBId, challengeData);
        var messageAd = ByteArrayUtils.JoinByteArrays(maskingIv, staticHeader.GetHeader());
        var encryptedMessage = packet[^staticHeader.EncryptedMessageLength..]; // This indexer statement extracts the encrypted message from the packet
        var decryptedMessage = AesCryptography.AesGcmDecrypt(sessionKeys.InitiatorKey, staticHeader.Nonce,
            encryptedMessage, messageAd);
        var pingMessage = PingMessage.DecodeMessage(decryptedMessage);
        var expectedRequestId = Convert.FromHexString("00000001");
        var expectedEnrSeq = 1;
        
        Assert.IsTrue(result);
        Assert.AreEqual(expectedRequestId, pingMessage.RequestId);
        Assert.AreEqual(expectedEnrSeq, pingMessage.EnrSeq);
    }
    
    [Test]
    public void Test_HandshakePingMessagePacketWithEnr_ShouldDecryptCorrectly()
    {
        var nodeAPubkey = new CryptoSession(Convert.FromHexString("eef77acb6c6a6eebc5b363a475ac583ec7eccdb42b6481424c60f59aa326547f")).GeneratePublicKey();
        var nodeBId = Convert.FromHexString("bbbb9d047f0488c0b5a93c1c3f2d8bafc7c8ff337024a55434a0d0555de64db9");
        var nodeBCrypto = new CryptoSession(Convert.FromHexString("66fb62bfbd66b9177a138c1e5cddbe4f7c30c343e94e68df8769459cb1cde628"));
        var packet = Convert.FromHexString(
            "00000000000000000000000000000000088b3d4342774649305f313964a39e55ea96c005ad539c8c7560413a7008f16c9e6d2f43bbea8814a546b7409ce783d34c4f53245d08da4bb23698868350aaad22e3ab8dd034f548a1c43cd246be98562fafa0a1fa86d8e7a3b95ae78cc2b988ded6a5b59eb83ad58097252188b902b21481e30e5e285f19735796706adff216ab862a9186875f9494150c4ae06fa4d1f0396c93f215fa4ef524e0ed04c3c21e39b1868e1ca8105e585ec17315e755e6cfc4dd6cb7fd8e1a1f55e49b4b5eb024221482105346f3c82b15fdaae36a3bb12a494683b4a3c7f2ae41306252fed84785e2bbff3b022812d0882f06978df84a80d443972213342d04b9048fc3b1d5fcb1df0f822152eced6da4d3f6df27e70e4539717307a0208cd208d65093ccab5aa596a34d7511401987662d8cf62b139471");
        var decryptedData = AesCryptography.AesCtrDecrypt(nodeBId[..16], packet);
        var staticHeader = StaticHeader.DecodeFromBytes(decryptedData);
        var handshakePacket = HandshakePacket.DecodeAuthData(staticHeader.AuthData);
        
        // If it contains a record, it verifies the record's signature 
        // Then, it verifies the 'id-signature' by extracting ephemeral public key from the handshake packet and using nodeB challenge data
        var enr = EnrRecordExtensions.FromBytes(handshakePacket.Record!);
        var enrRecordSignatureVerify = IdentitySchemeV4.VerifyEnrRecord(enr);
        var idSignature = handshakePacket.IdSignature;
        var maskingIv = packet[..16];
        var challengeData =
            Convert.FromHexString(
                "000000000000000000000000000000006469736376350001010102030405060708090a0b0c00180102030405060708090a0b0c0d0e0f100000000000000000");
        var ephemeralPubKey = handshakePacket.EphPubkey;
        var idSignatureVerify = CryptoSession.VerifyIdSignature(idSignature, nodeAPubkey, challengeData, ephemeralPubKey, nodeBId);
        var sharedSecret = nodeBCrypto.GenerateSharedSecret(ephemeralPubKey);
        var sessionKeys = CryptoSession.GenerateKeyDataFromSecret(sharedSecret, handshakePacket.SrcId!, nodeBId, challengeData);
        var messageAd = ByteArrayUtils.JoinByteArrays(maskingIv, staticHeader.GetHeader());
        var encryptedMessage = packet[^staticHeader.EncryptedMessageLength..]; // This indexer statement extracts the encrypted message from the packet
        var decryptedMessage = AesCryptography.AesGcmDecrypt(sessionKeys.InitiatorKey, staticHeader.Nonce,
            encryptedMessage, messageAd);
        var pingMessage = PingMessage.DecodeMessage(decryptedMessage);
        var expectedRequestId = Convert.FromHexString("00000001");
        var expectedEnrSeq = 1;
        
        Assert.IsTrue(enrRecordSignatureVerify);
        Assert.IsTrue(idSignatureVerify);
        Assert.AreEqual(expectedRequestId, pingMessage.RequestId);
        Assert.AreEqual(expectedEnrSeq, pingMessage.EnrSeq);
    }

    [Test]
    public void Test_Ecdh_ShouldGenerateSharedSecretCorrectly()
    {
        var publicKey = Convert.FromHexString("039961e4c2356d61bedb83052c115d311acb3a96f5777296dcf297351130266231");
        var secretKey = Convert.FromHexString("fb757dc581730490a1d7a00deea65e9b1936924caaea8f44d476014856b68736");
        var expectedSharedSecret =
            Convert.FromHexString("033b11a2a1f214567e1537ce5e509ffd9b21373247f2a3ff6841f4976f53165e7e");
        var sharedSecret = CryptoSession.GenerateSharedSecret(publicKey, secretKey);
        Assert.IsTrue(sharedSecret.SequenceEqual(expectedSharedSecret));
    }

    [Test]
    public void Test_InitiatorAndRecipientKeyGeneration_ShouldGenerateCorrectly()
    {
        var ephemeralKey = Convert.FromHexString("fb757dc581730490a1d7a00deea65e9b1936924caaea8f44d476014856b68736");
        var destPubkey = Convert.FromHexString("0317931e6e0840220642f230037d285d122bc59063221ef3226b1f403ddc69ca91");
        var nodeIdA = Convert.FromHexString("aaaa8419e9f49d0083561b48287df592939a8d19947d8c0ef88f2a4856a69fbb");
        var nodeIdB = Convert.FromHexString("bbbb9d047f0488c0b5a93c1c3f2d8bafc7c8ff337024a55434a0d0555de64db9");
        var challengeData =
            Convert.FromHexString(
                "000000000000000000000000000000006469736376350001010102030405060708090a0b0c00180102030405060708090a0b0c0d0e0f100000000000000000");
        var sharedSecret =  CryptoSession.GenerateSharedSecret(destPubkey, ephemeralKey);
        var initiatorKey = CryptoSession.GenerateKeyDataFromSecret(sharedSecret, nodeIdA, nodeIdB, challengeData).InitiatorKey;
        var recipientKey = CryptoSession.GenerateKeyDataFromSecret(sharedSecret, nodeIdA, nodeIdB, challengeData).RecipientKey;
        Assert.IsTrue(initiatorKey.SequenceEqual(Convert.FromHexString("dccc82d81bd610f4f76d3ebe97a40571")));
        Assert.IsTrue(recipientKey.SequenceEqual(Convert.FromHexString("ac74bb8773749920b0d3a8881c173ec5")));
    }

    [Test]
    public void Test_IdSignatureGeneration_ShouldCreateSignatureCorrectly()
    {
        var staticKey = Convert.FromHexString("fb757dc581730490a1d7a00deea65e9b1936924caaea8f44d476014856b68736");
        var session = new CryptoSession(staticKey);
        var challengeData = Convert.FromHexString("000000000000000000000000000000006469736376350001010102030405060708090a0b0c00180102030405060708090a0b0c0d0e0f100000000000000000");
        var emphemeralPubkey =
            Convert.FromHexString("039961e4c2356d61bedb83052c115d311acb3a96f5777296dcf297351130266231");
        var nodeBId = Convert.FromHexString("bbbb9d047f0488c0b5a93c1c3f2d8bafc7c8ff337024a55434a0d0555de64db9");
        var signature = session.GenerateIdSignature(challengeData, emphemeralPubkey, nodeBId);
        var expectedSignature = Convert.FromHexString("94852a1e2318c4e5e9d422c98eaf19d1d90d876b29cd06ca7cb7546d0fff7b484fe86c09a064fe72bdbef73ba8e9c34df0cd2b53e9d65528c2c7f336d5dfc6e6");
        Assert.IsTrue(signature.SequenceEqual(expectedSignature));
    }
    
    [Test]
    public void Test_IdSignatureVerification_ShouldVerifySignatureCorrectly()
    {
        var staticKey = Convert.FromHexString("fb757dc581730490a1d7a00deea65e9b1936924caaea8f44d476014856b68736");
        var cryptoSession = new CryptoSession(staticKey);
        var challengeData = Convert.FromHexString("000000000000000000000000000000006469736376350001010102030405060708090a0b0c00180102030405060708090a0b0c0d0e0f100000000000000000");
        var emphemeralPubkey =
            Convert.FromHexString("039961e4c2356d61bedb83052c115d311acb3a96f5777296dcf297351130266231");
        var nodeBId = Convert.FromHexString("bbbb9d047f0488c0b5a93c1c3f2d8bafc7c8ff337024a55434a0d0555de64db9"); 
        var signature = Convert.FromHexString("94852a1e2318c4e5e9d422c98eaf19d1d90d876b29cd06ca7cb7546d0fff7b484fe86c09a064fe72bdbef73ba8e9c34df0cd2b53e9d65528c2c7f336d5dfc6e6");
        var result = CryptoSession.VerifyIdSignature(signature, cryptoSession.GeneratePublicKey() ,challengeData, emphemeralPubkey, nodeBId);
        Assert.AreEqual(true, result);
    }
    
    [Test]
    public void Test_AesGcmEncryptionAndDecryption_ShouldEncryptAndDecryptCorrectly()
    {
        var key = Convert.FromHexString("9f2d77db7004bf8a1a85107ac686990b");
        var nonce = Convert.FromHexString("27b5af763c446acd2749fe8e");
        var msg = Convert.FromHexString("01c20101");
        var ad = Convert.FromHexString("93a7400fa0d6a694ebc24d5cf570f65d04215b6ac00757875e3f3a5f42107903");
        var cipher = AesCryptography.AesGcmEncrypt(key, nonce, msg, ad);
        var decrypted = AesCryptography.AesGcmDecrypt(key, nonce, cipher, ad);
        Console.WriteLine(Convert.ToHexString(cipher));
        Assert.IsTrue(msg.SequenceEqual(decrypted));
    }

    [Test]
    public void Test()
    {
        var ephemeralKey = Convert.FromHexString("0288ef00023598499cb6c940146d050d2b1fb914198c327f76aad590bead68b6");
        var session = new CryptoSession(ephemeralKey);
        var newPubKey = Convert.ToHexString(session.GeneratePublicKey());
        
        Console.WriteLine(newPubKey);
    }
}