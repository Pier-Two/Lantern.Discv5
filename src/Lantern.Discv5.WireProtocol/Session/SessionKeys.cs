using NBitcoin.Secp256k1;

namespace Lantern.Discv5.WireProtocol.Session;

public class SessionKeys : ISessionKeys
{
    public SessionKeys(byte[] privateKey, byte[]? ephemeralPubkey = null)
    {
        PrivateKey = CryptoContext.CreateECPrivKey(privateKey);
        EphemeralPrivateKey = CryptoContext.CreateECPrivKey(ephemeralPubkey ?? SessionUtility.GenerateRandomPrivateKey());
    }

    public ECPrivKey PrivateKey { get; } 
    
    public ECPrivKey EphemeralPrivateKey { get; }
    
    public Context CryptoContext { get; } = Context.Instance;
}