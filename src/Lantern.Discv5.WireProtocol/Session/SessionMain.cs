using Lantern.Discv5.Enr;
using Lantern.Discv5.Enr.EnrContent;
using Lantern.Discv5.Enr.EnrContent.Entries;
using Lantern.Discv5.Enr.IdentityScheme.V4;
using Lantern.Discv5.Rlp;
using Lantern.Discv5.WireProtocol.Packet;
using Lantern.Discv5.WireProtocol.Packet.Headers;
using Lantern.Discv5.WireProtocol.Packet.Types;

namespace Lantern.Discv5.WireProtocol.Session;

public class SessionMain
{
    private readonly ISessionKeys _sessionKeys;
    private readonly IAesUtility _aesUtility;
    private readonly ISessionCrypto _sessionCrypto;
    private readonly SessionType _sessionType;
    private byte[]? _challengeData;
    private SharedKeys? _currentSharedKeys;

    public bool IsEstablished { get; private set; }
    
    public SessionMain(ISessionKeys sessionKeys, IAesUtility aesUtility, ISessionCrypto sessionCrypto, SessionType sessionType)
    {
        _sessionKeys = sessionKeys;
        _aesUtility = aesUtility;
        _sessionCrypto = sessionCrypto;
        _sessionType = sessionType;
    }

    public byte[] PublicKey => _sessionKeys.PublicKey;

    public byte[] EphemeralPublicKey => _sessionKeys.EphemeralPublicKey;

    public void SetChallengeData(byte[] maskingIv, byte[] header)
    {
        _challengeData = ByteArrayUtils.JoinByteArrays(maskingIv, header);
    }
    
    public byte[]? GenerateIdSignature(byte[] destNodeId)
    {
        if (_challengeData == null)
        {
            Console.WriteLine("Challenge data is not set. Cannot generate id signature.");
            return null;
        }
        
        return _sessionCrypto.GenerateIdSignature(_sessionKeys, _challengeData, EphemeralPublicKey, destNodeId);
    }

    public bool VerifyIdSignature(HandshakePacketBase handshakePacket, byte[] publicKey, byte[] selfNodeId)
    {
        if (_challengeData == null)
        {
            Console.WriteLine("Challenge data is not set. Cannot generate id signature.");
            return false;
        }
        
        return _sessionCrypto.VerifyIdSignature(handshakePacket.IdSignature, _challengeData,publicKey, handshakePacket.EphPubkey, selfNodeId, _sessionKeys.CryptoContext);
    }

    public byte[]? EncryptMessageWithNewKeys(EnrRecord destRecord, StaticHeader header, byte[] selfNodeId, byte[] message, byte[] maskingIv)
    {
        var publicKey = destRecord.GetEntry<EntrySecp256K1>(EnrContentKey.Secp256K1).Value;
        var destNodeId = new IdentitySchemeV4Verifier().GetNodeIdFromRecord(destRecord);
        var sharedSecret = GenerateSharedSecret(_sessionKeys.EphemeralPrivateKey, publicKey);
        var messageAd = ByteArrayUtils.JoinByteArrays(maskingIv, header.GetHeader());
        
        if(_challengeData == null)
        {
            Console.WriteLine("Challenge data is not set. Cannot encrypt message.");
            return null;
        }
        
        _currentSharedKeys = _sessionCrypto.GenerateSessionKeys(sharedSecret, selfNodeId, destNodeId, _challengeData);

        return _aesUtility.AesGcmEncrypt(_currentSharedKeys.InitiatorKey, header.Nonce, message, messageAd);
    }

    public byte[]? DecryptMessageWithNewKeys(PacketMain packet, HandshakePacketBase handshakePacket, byte[] selfNodeId)
    {
        var sharedSecret = GenerateSharedSecret(_sessionKeys.PrivateKey, handshakePacket.EphPubkey);
        
        if (handshakePacket.SrcId == null)
        {
            Console.WriteLine("Handshake packet does not contain a source node id. Cannot decrypt packet.");
            return null;
        }
        
        if(_challengeData == null)
        {
            Console.WriteLine("Challenge data is not set. Cannot decrypt message.");
            return null;
        }
        
        var sharedKeys = _sessionCrypto.GenerateSessionKeys(sharedSecret, handshakePacket.SrcId, selfNodeId, _challengeData);
        var messageAd = ByteArrayUtils.JoinByteArrays(packet.MaskingIv, packet.StaticHeader.GetHeader());
        var decryptedResult = _aesUtility.AesGcmDecrypt(sharedKeys.InitiatorKey, packet.StaticHeader.Nonce, packet.EncryptedMessage, messageAd);
        
        _currentSharedKeys = sharedKeys;
        
        return decryptedResult;
    }

    public byte[]? EncryptMessage(StaticHeader header, byte[] maskingIv, byte[] rawMessage) 
    {
        if(_currentSharedKeys == null)
        {
            Console.WriteLine("SessionMain keys are not available. Cannot encrypt message.");
            return null;
        }
        
        var encryptionKey = _sessionType == SessionType.Initiator ? _currentSharedKeys.InitiatorKey : _currentSharedKeys.RecipientKey;
        var messageAd = ByteArrayUtils.JoinByteArrays(maskingIv, header.GetHeader());
        var encryptedMessage = _aesUtility.AesGcmEncrypt(encryptionKey, header.Nonce, rawMessage, messageAd);
        
        return encryptedMessage;
    }
    
    public byte[]? DecryptMessage(PacketMain packet)
    {
        if(_currentSharedKeys == null)
        {
            Console.WriteLine("SessionMain keys are not available. Cannot decrypt message.");
            return null;
        }
        
        var messageAd = ByteArrayUtils.JoinByteArrays(packet.MaskingIv, packet.StaticHeader.GetHeader());
        var decryptionKey = _sessionType == SessionType.Initiator ? _currentSharedKeys.RecipientKey : _currentSharedKeys.InitiatorKey;
        var decryptedMessage = _aesUtility.AesGcmDecrypt(decryptionKey, packet.StaticHeader.Nonce, packet.EncryptedMessage, messageAd);

        if (!IsEstablished)
        {
            IsEstablished = true;
        }
        
        return decryptedMessage;
    }

    private byte[] GenerateSharedSecret(byte[] privateKey, byte[] publicKey) =>
        _sessionCrypto.GenerateSharedSecret(privateKey, publicKey, _sessionKeys.CryptoContext);
}