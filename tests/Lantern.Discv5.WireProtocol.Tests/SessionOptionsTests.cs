using Lantern.Discv5.Enr.IdentityScheme.V4;
using Lantern.Discv5.WireProtocol.Session;
using Lantern.Discv5.WireProtocol.Utility;
using NUnit.Framework;

namespace Lantern.Discv5.WireProtocol.Tests;

public class SessionOptionsTests
{
    private SessionOptions _sessionOptions = null!;
    
    [Test]
    public void Test_SessionOptions_CreateDefault()
    {
        _sessionOptions = SessionOptions.Default;
        
        Assert.NotNull(_sessionOptions);
        Assert.NotNull(_sessionOptions.Signer);
        Assert.NotNull(_sessionOptions.Verifier);
        Assert.NotNull(_sessionOptions.SessionKeys);
        Assert.AreEqual(1000, _sessionOptions.CacheSize);
    }
    
    [Test]
    public void Test_SessionOptions_Builder()
    {
        var privateKey = RandomUtility.GenerateRandomData(32);
        var signer = new IdentitySchemeV4Signer(privateKey);
        var verifier = new IdentitySchemeV4Verifier();
        var sessionKeys = new SessionKeys(privateKey);
        
        _sessionOptions = new SessionOptions.Builder()
            .WithSigner(signer)
            .WithVerifier(verifier)
            .WithSessionKeys(sessionKeys)
            .WithCacheSize(2000)
            .Build();
        
        Assert.NotNull(_sessionOptions);
        Assert.NotNull(_sessionOptions.Signer);
        Assert.NotNull(_sessionOptions.Verifier);
        Assert.NotNull(_sessionOptions.SessionKeys);
        Assert.AreEqual(signer, _sessionOptions.Signer);
        Assert.AreEqual(verifier, _sessionOptions.Verifier);
        Assert.AreEqual(sessionKeys, _sessionOptions.SessionKeys);
        Assert.AreEqual(2000, _sessionOptions.CacheSize);
    }
}