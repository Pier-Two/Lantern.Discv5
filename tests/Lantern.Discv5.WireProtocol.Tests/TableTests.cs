using Lantern.Discv5.WireProtocol.Table;
using NUnit.Framework;

namespace Lantern.Discv5.WireProtocol.Tests;

[TestFixture]
public class TableTests
{
    [Test]
    public void Test1()
    {
        var firstNodeId = Convert.FromHexString("4E477ADAB7430EFEDCC8570C7244A8324EBA9034878862A9BD4FE2B7D19E5E39");
        var secondNodeId = Convert.FromHexString("4E46DE6028F1B6E80719F2067CCCA57D279C8247C9811A7C4A5B4CF54A9BD405");
        var firstDistance = TableUtility.Log2Distance(firstNodeId, secondNodeId);
        Console.WriteLine(firstDistance);
    }
}