using System.Net;
using Lantern.Discv5.Enr;
using Lantern.Discv5.Enr.Entries;
using Lantern.Discv5.WireProtocol.Connection;
using Lantern.Discv5.WireProtocol.Session;
using Lantern.Discv5.WireProtocol.Table;
using Lantern.Discv5.WireProtocol.Utility;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Lantern.Discv5.WireProtocol.Tests;

public class Discv5ProtocolTests
{
    private Discv5Protocol _discv5Protocol = null!;
    private ServiceProvider _serviceProvider = null!;

    [SetUp]
    public void Setup()
    {
        var bootstrapEnrs = new[]
        {
            "enr:-Ku4QImhMc1z8yCiNJ1TyUxdcfNucje3BGwEHzodEZUan8PherEo4sF7pPHPSIB1NNuSg5fZy7qFsjmUKs2ea1Whi0EBh2F0dG5ldHOIAAAAAAAAAACEZXRoMpD1pf1CAAAAAP__________gmlkgnY0gmlwhBLf22SJc2VjcDI1NmsxoQOVphkDqal4QzPMksc5wnpuC3gvSC8AfbFOnZY_On34wIN1ZHCCIyg",
            "enr:-Le4QPUXJS2BTORXxyx2Ia-9ae4YqA_JWX3ssj4E_J-3z1A-HmFGrU8BpvpqhNabayXeOZ2Nq_sbeDgtzMJpLLnXFgAChGV0aDKQtTA_KgEAAAAAIgEAAAAAAIJpZIJ2NIJpcISsaa0Zg2lwNpAkAIkHAAAAAPA8kv_-awoTiXNlY3AyNTZrMaEDHAD2JKYevx89W0CcFJFiskdcEzkH_Wdv9iW42qLK79ODdWRwgiMohHVkcDaCI4I"
        };
        
        var connectionOptions = new ConnectionOptions
        {
            Port = new Random().Next(1, 65535)
        };
        var sessionOptions = SessionOptions.Default;
        var tableOptions = new TableOptions(bootstrapEnrs);
        var enr = new EnrBuilder()
            .WithIdentityScheme(sessionOptions.Verifier, sessionOptions.Signer)
            .WithEntry(EnrEntryKey.Id, new EntryId("v4"))
            .WithEntry(EnrEntryKey.Secp256K1, new EntrySecp256K1(sessionOptions.Signer.PublicKey));
        var services = new ServiceCollection();
        
        services.AddDiscv5(builder =>
        {
            builder.WithConnectionOptions(connectionOptions)
                   .WithTableOptions(tableOptions)
                   .WithSessionOptions(sessionOptions)
                   .WithEnrBuilder(enr)
                   .Build();
        });

        _serviceProvider = services.BuildServiceProvider();
        _discv5Protocol = _serviceProvider.GetRequiredService<Discv5Protocol>();
    }
    
    [Test]
    public async Task Test_Discv5Protocol_PerformLookupAsync()
    {
        await _discv5Protocol.StartProtocolAsync();

        var firstClosestNodes = await _discv5Protocol.PerformLookupAsync(RandomUtility.GenerateRandomData(32));
        
        if (firstClosestNodes != null)
        {
            foreach (var node in firstClosestNodes)
            {
                Console.WriteLine("Closest node: " + Convert.ToHexString(node.Id));
            }
        }

        await _discv5Protocol.StopProtocolAsync();
    }
}