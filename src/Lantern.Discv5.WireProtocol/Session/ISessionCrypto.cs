using Lantern.Discv5.WireProtocol.Packet.Types;
using NBitcoin.Secp256k1;

namespace Lantern.Discv5.WireProtocol.Session;

public interface ISessionCrypto
{
    public SharedKeys GenerateSessionKeys(byte[] sharedSecret, byte[] nodeIdA, byte[] nodeIdB,
        byte[] challengeData);

    public byte[] GenerateIdSignature(ISessionKeys sessionKeys, byte[] challengeData, byte[] ephemeralPubkey,
        byte[] nodeId);

    public bool VerifyIdSignature(byte[] idSignature, byte[] challengeData, byte[] publicKey, byte[] ephPubKey,
        byte[] selfNodeId, Context cryptoContext);

    public byte[] GenerateSharedSecret(byte[] privateKey, byte[] publicKey, Context cryptoContext);
}