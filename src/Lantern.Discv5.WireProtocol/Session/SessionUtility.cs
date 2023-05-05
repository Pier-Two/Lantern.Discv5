using System.Security.Cryptography;
using System.Text;
using Lantern.Discv5.Rlp;
using NBitcoin.Secp256k1;
using SHA256 = System.Security.Cryptography.SHA256;

namespace Lantern.Discv5.WireProtocol.Session;

public static class SessionUtility
{
    private const int EcPrivateKeySize = 32;
    
    public static byte[] GenerateRandomPrivateKey()
    {
        using var rng = RandomNumberGenerator.Create();
        var randomBytes = new byte[EcPrivateKeySize];
        rng.GetBytes(randomBytes);
        return randomBytes;
    }
    
    public static byte[] GenerateSharedSecret(byte[] destPublicKey, byte[] localEphemeralPrivateKey, Context cryptoContext)
    {
        var remotePublicKey = cryptoContext.CreatePubKey(destPublicKey);
        var localPrivateKey = cryptoContext.CreateECPrivKey(localEphemeralPrivateKey);
        var sharedSecret = remotePublicKey.GetSharedPubkey(localPrivateKey);
        return sharedSecret.ToBytes();
    }

    public static bool VerifyIdSignature(byte[] idSignature, byte[] publicKey, byte[] challengeData, byte[] ephemeralPubkey, byte[] nodeId, Context cryptoContext)
    {
        var idSignatureText = Encoding.UTF8.GetBytes(SessionConstants.IdSignatureProof);
        var idSignatureInput = ByteArrayUtils.Concatenate(idSignatureText, challengeData, ephemeralPubkey, nodeId);
        var hash = SHA256.HashData(idSignatureInput);
        var key = cryptoContext.CreatePubKey(publicKey);
        return SecpECDSASignature.TryCreateFromCompact(idSignature, out var signature) && key.SigVerify(signature, hash);
    }

    public static SharedSessionKeys GenerateSessionKeys(byte[] sharedSecret, byte[] nodeIdA, byte[] nodeIdB, byte[] challengeData)
    {
        var kdfInfo = ByteArrayUtils.Concatenate(Encoding.UTF8.GetBytes(SessionConstants.DiscoveryAgreement), nodeIdA, nodeIdB);
        var prk = HKDF.Extract(HashAlgorithmName.SHA256, sharedSecret, challengeData);
        var keyData = new byte[EcPrivateKeySize];
        HKDF.Expand(HashAlgorithmName.SHA256, prk, keyData, kdfInfo);
        return new SharedSessionKeys(keyData);
    }
}