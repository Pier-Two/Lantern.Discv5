using Epoche;
using Lantern.Discv5.Enr.EntryType;
using NBitcoin.Secp256k1;

namespace Lantern.Discv5.Enr;

public class V4IdentityScheme : IEnrIdentityScheme
{
    private readonly ECPrivKey? _privateKey;

    public V4IdentityScheme(byte[] privateKey)
    {
        _privateKey = Context.Instance.CreateECPrivKey(privateKey);
    }

    public byte[] SignEnrRecord(EnrRecord record)
    {
        if (_privateKey == null) throw new InvalidOperationException("Private key must be provided before signing.");

        _privateKey.TrySignECDSA(Keccak256.ComputeHash(record.EncodeContent()), out var signature);

        if (signature == null) throw new InvalidOperationException("Failed to sign ENR record.");

        return signature.r.ToBytes().Concat(signature.s.ToBytes()).ToArray();
    }

    public static bool VerifyEnrRecord(EnrRecord record)
    {
        var publicKeyBytes = record.GetEntry<EntrySecp256K1>(EnrContentKey.Secp256K1).Value;
        var publicKey = Context.Instance.CreatePubKey(publicKeyBytes);
        SecpECDSASignature.TryCreateFromCompact(record.Signature, out var signature);

        if (signature == null) throw new InvalidOperationException("Failed to verify ENR record.");

        return publicKey.SigVerify(signature, Keccak256.ComputeHash(record.EncodeContent()));
    }

    public static byte[] DeriveNodeId(EnrRecord record)
    {
        var publicKeyBytes = record.GetEntry<EntrySecp256K1>(EnrContentKey.Secp256K1).Value;
        var publicKey = Context.Instance.CreatePubKey(publicKeyBytes);
        var xBytes = publicKey.Q.x.ToBytes();
        var yBytes = publicKey.Q.y.ToBytes();
        var publicKeyUncompressed = xBytes.Concat(yBytes);
        return Keccak256.ComputeHash(publicKeyUncompressed.ToArray());
    }
}