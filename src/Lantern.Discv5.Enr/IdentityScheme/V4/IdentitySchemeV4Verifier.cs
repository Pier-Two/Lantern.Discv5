using Epoche;
using Lantern.Discv5.Enr.EnrContent;
using Lantern.Discv5.Enr.EnrContent.Entries;
using Lantern.Discv5.Enr.IdentityScheme.Interfaces;
using NBitcoin.Secp256k1;

namespace Lantern.Discv5.Enr.IdentityScheme.V4;

public class IdentitySchemeV4Verifier : IIdentitySchemeVerifier
{
    public bool VerifyRecord(EnrRecord record)
    {
        var publicKeyBytes = record.GetEntry<EntrySecp256K1>(EnrContentKey.Secp256K1).Value;
        var publicKey = Context.Instance.CreatePubKey(publicKeyBytes);
        SecpECDSASignature.TryCreateFromCompact(record.Signature, out var signature);

        if (signature == null) throw new InvalidOperationException("Failed to verify ENR record.");

        return publicKey.SigVerify(signature, Keccak256.ComputeHash(record.EncodeContent()));
    }

    public byte[] GetNodeIdFromRecord(EnrRecord record)
    {
        var publicKeyBytes = record.GetEntry<EntrySecp256K1>(EnrContentKey.Secp256K1).Value;
        var publicKey = Context.Instance.CreatePubKey(publicKeyBytes);
        var xBytes = publicKey.Q.x.ToBytes();
        var yBytes = publicKey.Q.y.ToBytes();
        var publicKeyUncompressed = xBytes.Concat(yBytes);
        return Keccak256.ComputeHash(publicKeyUncompressed.ToArray());
    }
}
