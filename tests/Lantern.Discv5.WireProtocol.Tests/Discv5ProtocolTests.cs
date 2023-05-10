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
            "enr:-LS4QC7QWjzQeUeB86n2rzV8qOheSdLrLdRfJ7i3N7NCO9C-MjwiNSYJ3r9JOo57i1IuwoMVwv-stHvOpz3dnDBJ-bWCAx-HYXR0bmV0c4gAAACAAAAAgIRldGgykLuk2pYDAAAA__________-CaWSCdjSCaXCEQWzLM4lzZWNwMjU2azGhAztLki-cmCsgtKw3AuPAyKnp4dJEicV2cDWGeon6P4Mwg3RjcIInEIN1ZHCCJxA",
            "enr:-L24QEsldViw7HFYp1GHcGwp5Y3XbyYoOjMNlE_tYI_mm9MgUHFYeiBI0BEbrqIV3j3BYeS13ddj9zX_5ALUYmLXUA6BtodhdHRuZXRziP__________hGV0aDKQYolB7wMAECD__________4JpZIJ2NIJpcIQtOnAViXNlY3AyNTZrMaEC_iXGZDThJigM9OS0DQpuVxwm_DW8-IoDsbzK5cGLS4aIc3luY25ldHMPg3RjcIIjKIN1ZHCCIyg"
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