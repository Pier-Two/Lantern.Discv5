using Lantern.Discv5.Enr.Identity;
using Lantern.Discv5.Enr.Identity.V4;
using Lantern.Discv5.WireProtocol.Utility;

namespace Lantern.Discv5.WireProtocol.Session;

public class SessionOptions
{
    public IIdentitySigner Signer { get; private set; }
    public IIdentityVerifier Verifier { get; private set; }
    public ISessionKeys SessionKeys { get; private set; }
    public int CacheSize { get; private set; } = 1000;

    public static SessionOptions Default 
    {
        get
        {
            var privateKey = RandomUtility.GenerateRandomData(32);
            var signer = new IdentitySignerV4(privateKey);
            var verifier = new IdentityVerifierV4();
            var sessionKeys = new SessionKeys(privateKey);
                
            return new SessionOptions()
                .SetSigner(signer)
                .SetVerifier(verifier)
                .SetSessionKeys(sessionKeys);
        }
    }
        
    public SessionOptions SetSigner(IIdentitySigner signer)
    {
        Signer = signer;
        return this;
    }

    public SessionOptions SetVerifier(IIdentityVerifier verifier)
    {
        Verifier = verifier;
        return this;
    }

    public SessionOptions SetSessionKeys(ISessionKeys sessionKeys)
    {
        SessionKeys = sessionKeys;
        return this;
    }

    public SessionOptions SetCacheSize(int cacheSize)
    {
        CacheSize = cacheSize;
        return this;
    }
}