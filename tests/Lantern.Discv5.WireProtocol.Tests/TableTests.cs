using Lantern.Discv5.WireProtocol.Table;
using NUnit.Framework;

namespace Lantern.Discv5.WireProtocol.Tests;

[TestFixture]
public class TableTests
{
    [Test]
    public void Test1()
    {
        var firstNodeId = Convert.FromHexString("55b26bf7d902590c444d25a9408c2f01edb9268dc78b55e6afe5f41655c2d0eb");
        var secondNodeId = Convert.FromHexString("54444FC2F3722FFF0C05D618DA3637FFF2258E2BB01E9C5FEF34132BFF16F3F5");
        var firstDistance = TableUtility.Log2Distance(firstNodeId, secondNodeId);
        Console.WriteLine(firstDistance);
    }
}