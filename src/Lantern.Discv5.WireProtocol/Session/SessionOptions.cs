using Lantern.Discv5.Enr.Identity;
using Lantern.Discv5.Enr.Identity.V4;
using Lantern.Discv5.WireProtocol.Utility;

namespace Lantern.Discv5.WireProtocol.Session;

public class SessionOptions
{
    public IIdentitySigner Signer { get; }
    public IIdentityVerifier Verifier { get; }
    public ISessionKeys SessionKeys { get; }
    public int CacheSize { get; }

    private SessionOptions(Builder builder)
    {
        Signer = builder.Signer;
        Verifier = builder.Verifier;
        SessionKeys = builder.SessionKeys;
        CacheSize = builder.CacheSize;
    }

    public static SessionOptions Default => CreateDefault();
    
    private static SessionOptions CreateDefault()
    {
        var privateKey = RandomUtility.GenerateRandomData(32);
        var signer = new IdentitySignerV4(privateKey);
        var verifier = new IdentityVerifierV4();
        var sessionKeys = new SessionKeys(privateKey);
        
        return new Builder()
            .WithSigner(signer)
            .WithVerifier(verifier)
            .WithSessionKeys(sessionKeys)
            .Build();
    }
    
    public class Builder
    {
        public IIdentitySigner Signer { get; private set; }
        public IIdentityVerifier Verifier { get; private set; }
        public ISessionKeys SessionKeys { get; private set; }
        public int CacheSize { get; private set; } = 1000;

        public Builder WithSigner(IIdentitySigner signer)
        {
            Signer = signer;
            return this;
        }

        public Builder WithVerifier(IIdentityVerifier verifier)
        {
            Verifier = verifier;
            return this;
        }

        public Builder WithSessionKeys(ISessionKeys sessionKeys)
        {
            SessionKeys = sessionKeys;
            return this;
        }

        public Builder WithCacheSize(int cacheSize)
        {
            CacheSize = cacheSize;
            return this;
        }

        public SessionOptions Build()
        {
            return new SessionOptions(this);
        }
    }
}