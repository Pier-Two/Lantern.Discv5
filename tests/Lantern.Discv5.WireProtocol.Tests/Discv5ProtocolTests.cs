using Lantern.Discv5.Enr.EnrFactory;
using Lantern.Discv5.Enr.IdentityScheme.V4;
using Lantern.Discv5.WireProtocol.Connection;
using Lantern.Discv5.WireProtocol.Session;
using Lantern.Discv5.WireProtocol.Table;
using Lantern.Discv5.WireProtocol.Utility;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
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
            "enr:-Ku4QImhMc1z8yCiNJ1TyUxdcfNucje3BGwEHzodEZUan8PherEo4sF7pPHPSIB1NNuSg5fZy7qFsjmUKs2ea1Whi0EBh2F0dG5ldHOIAAAAAAAAAACEZXRoMpD1pf1CAAAAAP__________gmlkgnY0gmlwhBLf22SJc2VjcDI1NmsxoQOVphkDqal4QzPMksc5wnpuC3gvSC8AfbFOnZY_On34wIN1ZHCCIyg",
            "enr:-KG4QOtcP9X1FbIMOe17QNMKqDxCpm14jcX5tiOE4_TyMrFqbmhPZHK_ZPG2Gxb1GE2xdtodOfx9-cgvNtxnRyHEmC0ghGV0aDKQ9aX9QgAAAAD__________4JpZIJ2NIJpcIQDE8KdiXNlY3AyNTZrMaEDhpehBDbZjM_L9ek699Y7vhUJ-eAdMyQW_Fil522Y0fODdGNwgiMog3VkcIIjKA"
        };

        var bootstrapEnrsType = bootstrapEnrs
            .Select(enr => new EnrRecordFactory().CreateFromString(enr))
            .ToArray();
        
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder
                .AddFilter("Microsoft", LogLevel.Warning)
                .AddFilter("System", LogLevel.Warning)
                .AddSimpleConsole(options =>
                {
                    options.ColorBehavior = LoggerColorBehavior.Enabled;
                    options.IncludeScopes = false;
                    options.SingleLine = true;
                    options.TimestampFormat = "[HH:mm:ss] ";
                    options.UseUtcTimestamp = true;
                });
        });
        
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

        var services = ServiceConfiguration.ConfigureServices(loggerFactory, connectionOptions, sessionOptions, tableOptions);
        var serviceProvider = services.BuildServiceProvider();

        _discv5Protocol = new Discv5Protocol(serviceProvider);
    }
    
    [Test]
    public async Task Test()
    {
        var token = new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token;
         _discv5Protocol.StartProtocolAsync(token);

        var closestNodes = await _discv5Protocol.PerformLookup(RandomUtility.GenerateNodeId(32));

        if (closestNodes != null)
        {
            foreach (var node in closestNodes)
            {
                Console.WriteLine("Closest node: " + Convert.ToHexString(node.Id));
            }
        }
        
        await _discv5Protocol.StopProtocolAsync(token);
    }
} 