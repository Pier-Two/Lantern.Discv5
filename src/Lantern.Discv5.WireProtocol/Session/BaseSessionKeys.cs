using Lantern.Discv5.WireProtocol.Crypto;
using NBitcoin.Secp256k1;

namespace Lantern.Discv5.WireProtocol.Session;

public abstract class BaseSessionKeys : ISessionKeys
{
    protected BaseSessionKeys()
    {
        PrivateKey = GeneratePrivateKey();
        EphemeralPrivateKey = GeneratePrivateKey();
    }
    
    public ECPrivKey PrivateKey { get; protected init; } 
    
    public ECPrivKey EphemeralPrivateKey { get; protected init; }
    
    public Context CryptoContext { get; } = Context.Instance;

    protected ECPrivKey GeneratePrivateKey()
    {
        return CryptoContext.CreateECPrivKey(SessionUtils.GenerateRandomPrivateKey());
    }
}