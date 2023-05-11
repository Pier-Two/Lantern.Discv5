using Lantern.Discv5.Enr.EnrFactory;
using Lantern.Discv5.Enr.IdentityScheme.V4;
using Lantern.Discv5.WireProtocol.Connection;
using Lantern.Discv5.WireProtocol.Session;
using Lantern.Discv5.WireProtocol.Table;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Lantern.Discv5.WireProtocol.Tests;

public class Discv5ProtocolTests
{
    private Discv5Protocol _discv5Protocol = null!;
    
    [SetUp]
    public void Setup()
    {
        var privateKey = /*new SessionUtility().GenerateRandomPrivateKey();*/Convert.FromHexString("BAFA8BDEC1F4F02D227A2F99C5FD8F439B457942B466115A0FAB9FC3F9E97D67");
        var signer = new IdentitySchemeV4Signer(privateKey);
        var verifier = new IdentitySchemeV4Verifier();
        var sessionKeys = new SessionKeys(privateKey);
        var bootstrapEnrs = new[]
        {
            "enr:-MK4QGpWUNblM2aPMh4OJfsMr-3Y7gASClzZHGMcCllUNvuqMsHDgng2PSrWXGZcTp662aojSVhlc5sSKmg_7QaoWMKGAYd1kr0jh2F0dG5ldHOIAAAAAAAAAACEZXRoMpBiiUHvAwAQIP__________gmlkgnY0gmlwhANY2TSJc2VjcDI1NmsxoQIQ9k4vhofxXrxplaWjfwiojWGq7VLE2OelvabXkxjy14hzeW5jbmV0cwCDdGNwgjLIg3VkcIIu4A",
            "enr:-LS4QEcq_vps6TP--K6eU0qZRVSGWODiSjtjD8Qsl4khrmoNPt9bqnlRQCgFfVtOjyL0XDXaliRtdEbr5Vh6kJEAcZOCF2CHYXR0bmV0c4hzSRGFSRFBiIRldGgykLuk2pYDAAAA__________-CaWSCdjSCaXCEqFvunYlzZWNwMjU2azGhAj8sxGQdGnkzcvnlxfViCHz9rqK96lWkubDaK957SZQdg3RjcIIjKIN1ZHCCIyg"
        };
        
        var bootstrapEnrsType = bootstrapEnrs
            .Select(enr => new EnrRecordFactory().CreateFromString(enr))
            .ToArray();
        
        var connectionOptions = new ConnectionOptions.Builder()
            .WithExternalIpAddressAsync().Result
            .Build();

        var sessionOptions = new SessionOptions.Builder()
            .WithSigner(signer)
            .WithVerifier(verifier)
            .WithSessionKeys(sessionKeys)
            .WithCacheSize(100)
            .Build();

        var tableOptions = new TableOptions.Builder()
            .WithBootstrapEnrs(bootstrapEnrsType)
            .Build();

        var services = ServiceConfiguration.ConfigureServices(connectionOptions, sessionOptions, tableOptions);
        var serviceProvider = services.BuildServiceProvider();

        _discv5Protocol = new Discv5Protocol(serviceProvider);
    }
    
    [Test]
    public async Task Test()
    { 
        await _discv5Protocol.StartDiscoveryAsync();
    }
}