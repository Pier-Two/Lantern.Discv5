using Lantern.Discv5.Enr.IdentityScheme.Interfaces;
using Lantern.Discv5.Enr.IdentityScheme.V4;
using Lantern.Discv5.WireProtocol.Utility;

namespace Lantern.Discv5.WireProtocol.Session;

public class SessionOptions
{
    public IIdentitySchemeSigner Signer { get; }
    public IIdentitySchemeVerifier Verifier { get; }
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
        var privateKey = RandomUtility.GeneratePrivateKey(32);
        var signer = new IdentitySchemeV4Signer(privateKey);
        var verifier = new IdentitySchemeV4Verifier();
        var sessionKeys = new SessionKeys(privateKey);
        
        return new Builder()
            .WithSigner(signer)
            .WithVerifier(verifier)
            .WithSessionKeys(sessionKeys)
            .WithCacheSize(100)
            .Build();
    }
    
    public class Builder
    {
        public IIdentitySchemeSigner Signer { get; private set; }
        public IIdentitySchemeVerifier Verifier { get; private set; }
        public ISessionKeys SessionKeys { get; private set; } 
        public int CacheSize { get; private set; }

        public Builder WithSigner(IIdentitySchemeSigner signer)
        {
            Signer = signer;
            return this;
        }

        public Builder WithVerifier(IIdentitySchemeVerifier verifier)
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