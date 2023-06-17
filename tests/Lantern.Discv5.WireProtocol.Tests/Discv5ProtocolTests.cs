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
        
        _discv5Protocol = Discv5Builder.CreateDefault(bootstrapEnrs);
    }
    
    [Test]
    public async Task Test()
    {
        _discv5Protocol.StartProtocolAsync();

        var closestNodes = await _discv5Protocol.PerformLookupAsync(Convert.FromHexString("1888D1A446592EA3A41BCE3B36F5A2FFB72B8E5FCFCA6E75EB60C6686F269AF8"));

        if (closestNodes != null)
        {
            foreach (var node in closestNodes)
            {
                Console.WriteLine("Closest node: " + Convert.ToHexString(node.Id));
            }
        } 
        
        await _discv5Protocol.StopProtocolAsync();
    }
} 