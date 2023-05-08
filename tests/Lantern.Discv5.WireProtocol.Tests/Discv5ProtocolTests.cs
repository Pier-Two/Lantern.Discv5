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
        var privateKey = Convert.FromHexString("BAFA8BDEC1F4F02D227A2F99C5FD8F439B457942B466115A0FAB9FC3F9E97D67");//SessionUtility.GenerateRandomPrivateKey();
        var signer = new IdentitySchemeV4Signer(privateKey);
        var verifier = new IdentitySchemeV4Verifier();
        var sessionKeys = new SessionKeys(privateKey);
        var bootstrapEnrs = new[]
        {
            "enr:-LK4QLnkZV1BLaze436M59DsWnJkb1cD6Hr1bT45aqgZExv4DMPZk_QRuihxMMkYUolAghmt8U50qDpgapGorgv-Ff48h2F0dG5ldHOIAAAEAAAAAACEZXRoMpCCS-QxAgAAZP__________gmlkgnY0gmlwhJw7izuJc2VjcDI1NmsxoQKosgoCoL8nni_8gBXh2b1zbyfcTElOXCvv6fr3tbrZsoN0Y3CCIymDdWRwgiMp",
            "enr:-Ly4QKecnqhE18429F_xNxOawWrRF673vE3WxbKpFFvlCrw3DBnyAgwqvOn606ZFL0NX6zY2CkGMFWYrU7Z8_AkohmsEh2F0dG5ldHOIAAAAAAAAAACEZXRoMpC7pNqWAwAAAP__________gmlkgnY0gmlwhDZQj4KJc2VjcDI1NmsxoQLWa-UIglSK-K4-MiTdi6pEYhPwQYxJN4k2q6NMevQUhIhzeW5jbmV0cwCDdGNwgiMog3VkcIIjKA"
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