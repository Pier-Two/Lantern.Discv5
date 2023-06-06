using Lantern.Discv5.WireProtocol.Table;
using NUnit.Framework;

namespace Lantern.Discv5.WireProtocol.Tests;

[TestFixture]
public class TableTests
{
    [Test]
    public void Test1()
    {
        var firstNodeId = Convert.FromHexString("4A2E2A02F3DE741C7DB65A4E431BBD7FFF02C9C2AA3AD7E4E2999D906483EC4A");
        var secondNodeId = Convert.FromHexString("4A2E7054A27B7743D2200FC92080A468C1B352FE250886879E070BDEFDDA35CE");
        var firstDistance = TableUtility.Log2Distance(firstNodeId, secondNodeId);
        Console.WriteLine(firstDistance);
    }
}