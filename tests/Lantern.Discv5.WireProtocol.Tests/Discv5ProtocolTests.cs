using Lantern.Discv5.WireProtocol.Connection;
using Lantern.Discv5.WireProtocol.Session;
using Lantern.Discv5.WireProtocol.Utility;
using NUnit.Framework;

namespace Lantern.Discv5.WireProtocol.Tests;

public class Discv5ProtocolTests
{
    private Discv5Protocol _discv5Protocol = null!;

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
        
        _discv5Protocol = new Discv5ProtocolBuilder()
            .WithConnectionOptions(connectionOptions)
            .WithBootstrapEnrs(bootstrapEnrs)
            .Build();
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