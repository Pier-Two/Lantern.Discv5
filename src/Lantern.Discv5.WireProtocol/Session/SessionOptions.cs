using Lantern.Discv5.Enr.IdentityScheme.Interfaces;

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