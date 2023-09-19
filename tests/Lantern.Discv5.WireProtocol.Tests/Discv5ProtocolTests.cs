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
            "enr:-KG4QOtcP9X1FbIMOe17QNMKqDxCpm14jcX5tiOE4_TyMrFqbmhPZHK_ZPG2Gxb1GE2xdtodOfx9-cgvNtxnRyHEmC0ghGV0aDKQ9aX9QgAAAAD__________4JpZIJ2NIJpcIQDE8KdiXNlY3AyNTZrMaEDhpehBDbZjM_L9ek699Y7vhUJ-eAdMyQW_Fil522Y0fODdGNwgiMog3VkcIIjKA"
        };
        
        //_discv5Protocol = Discv5Builder.CreateDefault(bootstrapEnrs);
        _discv5Protocol = new Discv5Builder()
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