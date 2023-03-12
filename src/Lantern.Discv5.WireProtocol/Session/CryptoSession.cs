using System.Security.Cryptography;
using System.Text;
using Lantern.Discv5.Rlp;
using NBitcoin.Secp256k1;
using SHA256 = System.Security.Cryptography.SHA256;

namespace Lantern.Discv5.WireProtocol.Session;

public class CryptoSession
{
    private readonly ECPrivKey _privateKey;
    private readonly ECPrivKey _ephemeralPrivateKey;

    public CryptoSession()
    {
        var privateKeyRandomBytes = new byte[32];
        RandomNumberGenerator.Create().GetBytes(privateKeyRandomBytes);
        _privateKey = Context.Instance.CreateECPrivKey(privateKeyRandomBytes);
        
        var ephemeralPrivateKeyRandomBytes = new byte[32];
        RandomNumberGenerator.Create().GetBytes(ephemeralPrivateKeyRandomBytes);
        _ephemeralPrivateKey = Context.Instance.CreateECPrivKey(ephemeralPrivateKeyRandomBytes);
    }

    public CryptoSession(byte[] privateKey)
    {
        _privateKey = Context.Instance.CreateECPrivKey(privateKey);
        var randomBytes = new byte[32];
        RandomNumberGenerator.Create().GetBytes(randomBytes);
        _ephemeralPrivateKey = Context.Instance.CreateECPrivKey(randomBytes);
    }
    
    public CryptoSession(byte[] privateKey, byte[] ephemeralPrivateKey)
    {
        _privateKey = Context.Instance.CreateECPrivKey(privateKey);
        _ephemeralPrivateKey = Context.Instance.CreateECPrivKey(ephemeralPrivateKey);
    }

    public byte[] GeneratePublicKey()
    {
        return _privateKey.CreatePubKey().ToBytes();
    }
    
    public byte[] GenerateEphemeralPublicKey()
    {
        return _ephemeralPrivateKey.CreatePubKey().ToBytes();
    }
    
    public byte[] GenerateSharedSecret(byte[] ephemeralPublicKey)
    {
        var remotePublicKeyContext = Context.Instance.CreatePubKey(ephemeralPublicKey);
        var sharedPublicKey = remotePublicKeyContext.GetSharedPubkey(_privateKey);
        return sharedPublicKey.ToBytes();
    }

    public static byte[] GenerateSharedSecret(byte[] ephemeralPublicKey, byte[] ephemeralPrivateKey)
    {
        var remotePublicKeyContext = Context.Instance.CreatePubKey(ephemeralPublicKey);
        var remotePrivateKeyContext = Context.Instance.CreateECPrivKey(ephemeralPrivateKey);
        var sharedPublicKey = remotePublicKeyContext.GetSharedPubkey(remotePrivateKeyContext);
        return sharedPublicKey.ToBytes();
    }
    
    public byte[] GenerateIdSignature(byte[] challengeData, byte[] ephemeralPubkey, byte[] nodeId)
    {
        var idSignatureText = Encoding.UTF8.GetBytes(SessionConstants.IdSignatureProof);
        var idSignatureInput = Helpers.JoinMultipleByteArrays(idSignatureText, challengeData, ephemeralPubkey, nodeId);
        var hash = SHA256.HashData(idSignatureInput);
        _privateKey.TrySignECDSA(hash, out var signature);
        return signature!.r.ToBytes().Concat(signature.s.ToBytes()).ToArray();
    }
    
    public static bool VerifyIdSignature(byte[] idSignature, byte[] publicKey, byte[] challengeData, byte[] ephemeralPubkey, byte[] nodeId)
    {
        var idSignatureText = Encoding.UTF8.GetBytes(SessionConstants.IdSignatureProof);
        var idSignatureInput = Helpers.JoinMultipleByteArrays(idSignatureText, challengeData, ephemeralPubkey, nodeId);
        var hash = SHA256.HashData(idSignatureInput);
        var key = Context.Instance.CreatePubKey(publicKey);
        SecpECDSASignature.TryCreateFromCompact(idSignature, out var signature);
        return key.SigVerify(signature!, hash);
    }

    /*
    public byte[] GenerateKeyData(byte[] destPublicKey, byte[] nodeIdA, byte[] nodeIdB, byte[] challengeData)
    {
        var kdfInfo = Helpers.JoinMultipleByteArrays(Encoding.UTF8.GetBytes(SessionConstants.DiscoveryAgreement), nodeIdA, nodeIdB);
        var sharedSecret = GenerateSharedSecret(destPublicKey);
        var prk = HKDF.Extract(HashAlgorithmName.SHA256, sharedSecret, challengeData);
        var keyData = new byte[32];
        HKDF.Expand(HashAlgorithmName.SHA256, prk, keyData, kdfInfo);
        return keyData;
    }*/
    
    public static SessionKeys GenerateKeyDataFromSecret(byte[] sharedSecret, byte[] nodeIdA, byte[] nodeIdB, byte[] challengeData)
    {
        var kdfInfo = Helpers.JoinMultipleByteArrays(Encoding.UTF8.GetBytes(SessionConstants.DiscoveryAgreement), nodeIdA, nodeIdB);
        var prk = HKDF.Extract(HashAlgorithmName.SHA256, sharedSecret, challengeData);
        var keyData = new byte[32];
        HKDF.Expand(HashAlgorithmName.SHA256, prk, keyData, kdfInfo);
        return new SessionKeys(keyData);
    }
    
    /*
    private byte[] GenerateSharedSecret(byte[] ephemeralPublicKey)
    {
        var remotePublicKeyContext = Context.Instance.CreatePubKey(ephemeralPublicKey);
        var sharedPublicKey = remotePublicKeyContext.GetSharedPubkey(_ephemeralPrivateKey);
        return sharedPublicKey.ToBytes();
    }*/
}