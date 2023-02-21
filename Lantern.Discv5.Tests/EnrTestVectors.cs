using System.Net;
using Lantern.Discv5.Enr;
using Lantern.Discv5.Enr.EntryType;
using NUnit.Framework;

namespace Lantern.Discv5.Tests;

[TestFixture]
public class EnrTestVectors
{
    [TestCase(
        "enr:-IS4QHCYrYZbAKWCBRlAy5zzaDZXJBGkcnh4MHcBFZntXNFrdvJjX04jRzjzCBOonrkTfj499SZuOh8R33Ls8RRcy5wBgmlkgnY0gmlwhH8AAAGJc2VjcDI1NmsxoQPKY0yuDUmstAHYpMa2_oxVtw0RW_QAdpzBQA8yWM0xOIN1ZHCCdl8")]
    public void Test_EnrRecordSerialization_ShouldSerializeCorrectly(string enrString)
    {
        var enr = new EnrRecord();
        var signature =
            Convert.FromHexString(
                "7098ad865b00a582051940cb9cf36836572411a47278783077011599ed5cd16b76f2635f4e234738f30813a89eb9137e3e3df5266e3a1f11df72ecf1145ccb9c");
        var ip = new EntryIp(new IPAddress(Convert.FromHexString("7f000001")));
        var pubKey =
            new EntrySecp256K1(
                Convert.FromHexString("03ca634cae0d49acb401d8a4c6b6fe8c55b70d115bf400769cc1400f3258cd3138"));
        var udp = new EntryUdp(30303);

        enr.Signature = signature;
        enr.SequenceNumber = 1;
        enr.AddEntry(new EntryId("v4"));
        enr.AddEntry(ip);
        enr.AddEntry(pubKey);
        enr.AddEntry(udp);
        
        Assert.AreEqual(enrString, enr.ToString());
    }
    
    [TestCase(
        "enr:-IS4QHCYrYZbAKWCBRlAy5zzaDZXJBGkcnh4MHcBFZntXNFrdvJjX04jRzjzCBOonrkTfj499SZuOh8R33Ls8RRcy5wBgmlkgnY0gmlwhH8AAAGJc2VjcDI1NmsxoQPKY0yuDUmstAHYpMa2_oxVtw0RW_QAdpzBQA8yWM0xOIN1ZHCCdl8")]
    public void Test_EnrStringSerialization_ShouldSerializeCorrectly(string enrString)
    {
        var enr = EnrRecordExtensions.CreateEnrRecord(enrString);
        var signature =
            Convert.FromHexString(
                "7098ad865b00a582051940cb9cf36836572411a47278783077011599ed5cd16b76f2635f4e234738f30813a89eb9137e3e3df5266e3a1f11df72ecf1145ccb9c");
        Assert.AreEqual(signature, enr.Signature);
        Assert.AreEqual(1, enr.SequenceNumber);
        
        var id = enr.GetEntry("id")!.GetValue();
        Assert.AreEqual("v4", id);
        
        var ip = enr.GetEntry("ip")!.GetValue();
        Assert.AreEqual(new IPAddress(Convert.FromHexString("7f000001")), ip);
        
        var pubKey = enr.GetEntry("secp256k1")!.GetValue();
        Assert.AreEqual(Convert.FromHexString("03ca634cae0d49acb401d8a4c6b6fe8c55b70d115bf400769cc1400f3258cd3138"), pubKey);
        
        var udp = enr.GetEntry("udp")!.GetValue();
        Assert.AreEqual(  Utility.ByteArrayToUInt64(Convert.FromHexString("765f")), udp);
    }
    
}