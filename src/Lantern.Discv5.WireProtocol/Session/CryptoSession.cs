using System.Text;
using Lantern.Discv5.Rlp;
using Lantern.Discv5.WireProtocol.Crypto;
using NBitcoin.Secp256k1;
using SHA256 = System.Security.Cryptography.SHA256;

namespace Lantern.Discv5.WireProtocol.Session;

public class CryptoSession
{
    private readonly ECPrivKey _privateKey;
    private readonly ECPrivKey _ephemeralPrivateKey;
    private readonly ISessionKeys _sessionKeys;
    
    public CryptoSession(ISessionKeys sessionKeys)
    {
        _sessionKeys = sessionKeys;
        _privateKey = sessionKeys.PrivateKey;
        _ephemeralPrivateKey = sessionKeys.EphemeralPrivateKey;
    }
    
    public byte[] PublicKey => _privateKey.CreatePubKey().ToBytes();
    
    public byte[] EphemeralPublicKey => _ephemeralPrivateKey.CreatePubKey().ToBytes();
    
    public byte[] GenerateSharedSecret(byte[] remoteEphemeralPublicKey)
    {
        var remotePublicKey = _sessionKeys.CryptoContext.CreatePubKey(remoteEphemeralPublicKey);
        var sharedSecret = remotePublicKey.GetSharedPubkey(_privateKey);
        return sharedSecret.ToBytes();
    }
    
    public byte[] GenerateIdSignature(byte[] challengeData, byte[] ephemeralPubkey, byte[] nodeId)
    {
        var idSignatureText = Encoding.UTF8.GetBytes(SessionConstants.IdSignatureProof);
        var idSignatureInput = ByteArrayUtils.Concatenate(idSignatureText, challengeData, ephemeralPubkey, nodeId);
        var hash = SHA256.HashData(idSignatureInput);
        _privateKey.TrySignECDSA(hash, out var signature);
        return ConcatenateSignature(signature!);
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