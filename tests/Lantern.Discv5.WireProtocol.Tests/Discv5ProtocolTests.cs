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
        var privateKey = Convert.FromHexString("A90C6EB0A559E47C2C109B5A7826B947CA3787CF4503DBF4EDB0A08BD3C84FD5");
        var signer =
            new IdentitySchemeV4Signer(
                Convert.FromHexString("A90C6EB0A559E47C2C109B5A7826B947CA3787CF4503DBF4EDB0A08BD3C84FD5"));
        var verifier = new IdentitySchemeV4Verifier();
        var testSessionKeys = new BaseSessionKeys(privateKey);
        var bootstrapEnrs = new[]
        {
            "enr:-HW4QGGuWgs0oUYUf5R_oGzvgRF9ZoZYO2Ql827deQASncnLJI4QiR7y49WGHPq_2ynEkkQaeHwb4JyrJV9b3u7Ql_gBgmlkgnY0iXNlY3AyNTZrMaEC7shDw9kzX9w2Kgl9ZzyDo8zdlISuG-RmNr1ePmVUujk"
        };
        var bootstrapEnrsType = bootstrapEnrs
            .Select(enr => new EnrRecordFactory().CreateFromString(enr))
            .ToArray();
        
        var connectionOptions = new ConnectionOptions.Builder()
            .Build();

        var sessionOptions = new SessionOptions.Builder()
            .WithSigner(signer)
            .WithVerifier(verifier)
            .WithSessionKeys(testSessionKeys)
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
        await _discv5Protocol.StartServiceAsync();
    }
}