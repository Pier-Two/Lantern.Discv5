using NBitcoin.Secp256k1;

namespace Lantern.Discv5.WireProtocol.Session;

public interface ISessionKeys
{
    ECPrivKey PrivateKey { get; }
    
    ECPrivKey EphemeralPrivateKey { get; }
    
    Context CryptoContext { get; }
}