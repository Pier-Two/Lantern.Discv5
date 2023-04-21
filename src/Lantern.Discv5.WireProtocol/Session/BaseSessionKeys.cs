using Lantern.Discv5.WireProtocol.Utility;
using NBitcoin.Secp256k1;

namespace Lantern.Discv5.WireProtocol.Session;

public class BaseSessionKeys : ISessionKeys
{
    public BaseSessionKeys(byte[]? privateKey = null, byte[]? ephemeralPrivateKey = null)
    {
        PrivateKey = CryptoContext.CreateECPrivKey(privateKey ?? SessionUtils.GenerateRandomPrivateKey());
        EphemeralPrivateKey = CryptoContext.CreateECPrivKey(ephemeralPrivateKey ?? SessionUtils.GenerateRandomPrivateKey());
    }

    public ECPrivKey PrivateKey { get; } 
    
    public ECPrivKey EphemeralPrivateKey { get; }
    
    public Context CryptoContext { get; } = Context.Instance;

    private ECPrivKey GeneratePrivateKey()
    {
        return CryptoContext.CreateECPrivKey(SessionUtils.GenerateRandomPrivateKey());
    }
}