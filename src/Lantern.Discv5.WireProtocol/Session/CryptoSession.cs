using System.Security.Cryptography;
using System.Text;
using Lantern.Discv5.Rlp;
using Lantern.Discv5.WireProtocol.Utility;
using NBitcoin.Secp256k1;
using SHA256 = System.Security.Cryptography.SHA256;

namespace Lantern.Discv5.WireProtocol.Session;

public class CryptoSession
{
    private readonly ECPrivKey _privateKey;
    private readonly ECPrivKey _ephemeralPrivateKey;
    private readonly ISessionKeys _sessionKeys;
    private readonly SessionType _sessionType;
    private SharedSessionKeys? _sharedSessionKeys;
    private int? _outgoingMessageCounter;
    
    public byte[]? ChallengeData { get; set; }

    public SharedSessionKeys CurrentSessionKeys;
    public CryptoSession(ISessionKeys sessionKeys, SessionType sessionType)
    {
        _sessionKeys = sessionKeys;
        _privateKey = sessionKeys.PrivateKey;
        _ephemeralPrivateKey = sessionKeys.EphemeralPrivateKey;
        _sessionType = sessionType;
    }
    
    public byte[] PublicKey => _privateKey.CreatePubKey().ToBytes();
    
    public byte[] EphemeralPublicKey => _ephemeralPrivateKey.CreatePubKey().ToBytes();

    public byte[] GenerateIdSignature(byte[] challengeData, byte[] ephemeralPubkey, byte[] nodeId)
    {
        var idSignatureText = Encoding.UTF8.GetBytes(SessionConstants.IdSignatureProof);
        var idSignatureInput = ByteArrayUtils.Concatenate(idSignatureText, challengeData, ephemeralPubkey, nodeId);
        var hash = SHA256.HashData(idSignatureInput);
        _privateKey.TrySignECDSA(hash, out var signature);
        return ConcatenateSignature(signature!);
    }

    public byte[] GenerateSharedSecret(byte[] publicKey)
    {
        byte[] sharedSecret; 
        
        if (_sessionType == SessionType.Initiator)
        {
            sharedSecret = GenerateSharedSecretFromEphPrivateKey(publicKey);
        }
        else if(_sessionType == SessionType.Recipient)
        {
            sharedSecret = GenerateSharedSecretFromPrivateKey(publicKey);
        }
        else
        {
            throw new Exception("Invalid session type");
        }

        return sharedSecret;
    }
    
    public byte[] GenerateSharedSecretFromPrivateKey(byte[] remoteEphemeralPubKey)
    {
        var ephemeralPublicKey = _sessionKeys.CryptoContext.CreatePubKey(remoteEphemeralPubKey);
        var sharedSecret = ephemeralPublicKey.GetSharedPubkey(_privateKey).ToBytes();
        return sharedSecret;
    }
    
    public byte[] GenerateSharedSecretFromEphPrivateKey(byte[] destPubKey)
    {
        var remotePublicKey = _sessionKeys.CryptoContext.CreatePubKey(destPubKey);
        var sharedSecret = remotePublicKey.GetSharedPubkey(_ephemeralPrivateKey).ToBytes();
        return sharedSecret;
    }

    public void ResetOutgoingMessageCounter()
    {
        _outgoingMessageCounter = 0;
    }

    private static byte[] ConcatenateSignature(SecpECDSASignature signature)
    {
        var rBytes = signature.r.ToBytes();
        var sBytes = signature.s.ToBytes();
        var result = new byte[rBytes.Length + sBytes.Length];
        Buffer.BlockCopy(rBytes, 0, result, 0, rBytes.Length);
        Buffer.BlockCopy(sBytes, 0, result, rBytes.Length, sBytes.Length);
        return result;
    }
}