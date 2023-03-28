using Epoche;
using NBitcoin.Secp256k1;

namespace Lantern.Discv5.Enr.Identity;

public class IdentitySchemeV4Signer : IIdentitySchemeSigner
{
    private readonly ECPrivKey? _privateKey;

    public IdentitySchemeV4Signer(byte[] privateKey)
    {
        _privateKey = Context.Instance.CreateECPrivKey(privateKey);
    }

    public byte[] SignRecord(EnrRecord record)
    {
        if (_privateKey == null) throw new InvalidOperationException("Private key must be provided before signing.");

        _privateKey.TrySignECDSA(Keccak256.ComputeHash(record.EncodeContent()), out var signature);

        if (signature == null) throw new InvalidOperationException("Failed to sign ENR record.");

        return signature.r.ToBytes().Concat(signature.s.ToBytes()).ToArray();
    }
}