using Lantern.Discv5.Enr;
using NUnit.Framework;

namespace Lantern.Discv5.Tests;

[TestFixture]
public class RlpTests
{
    [Test]
    public void Test_RlpSerialization_ShouldSerializeCorrectly()
    {
        var signature =
            Convert.FromHexString(
                "7098ad865b00a582051940cb9cf36836572411a47278783077011599ed5cd16b76f2635f4e234738f30813a89eb9137e3e3df5266e3a1f11df72ecf1145ccb9c");

        var id = "id";
        var value = "v4";
        var Udp = 30303;

        Console.WriteLine(string.Join(", ", RlpExtensions.Encode(Udp).ToArray()));
    }
}