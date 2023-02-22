using System.Net;
using Lantern.Discv5.Enr;
using Lantern.Discv5.Enr.EntryType;
using NUnit.Framework;

namespace Lantern.Discv5.Tests;

[TestFixture]
public class EnrTestVectors
{
    [Test]
    public void Test_ConvertEnrRecordToEnrString_ShouldConvertCorrectly()
    {
        var enrString =
            "enr:-IS4QHCYrYZbAKWCBRlAy5zzaDZXJBGkcnh4MHcBFZntXNFrdvJjX04jRzjzCBOonrkTfj499SZuOh8R33Ls8RRcy5wBgmlkgnY0gmlwhH8AAAGJc2VjcDI1NmsxoQPKY0yuDUmstAHYpMa2_oxVtw0RW_QAdpzBQA8yWM0xOIN1ZHCCdl8";
        var enr = new EnrRecord();
        var signature =
            Convert.FromHexString(
                "7098ad865b00a582051940cb9cf36836572411a47278783077011599ed5cd16b76f2635f4e234738f30813a89eb9137e3e3df5266e3a1f11df72ecf1145ccb9c");
        var id = new EntryId("v4");
        var ip = new EntryIp(new IPAddress(Convert.FromHexString("7f000001")));
        var pubKey =
            new EntrySecp256K1(
                Convert.FromHexString("03ca634cae0d49acb401d8a4c6b6fe8c55b70d115bf400769cc1400f3258cd3138"));
        var udp = new EntryUdp(30303);

        enr.Signature = signature;
        enr.SequenceNumber = 1;
        enr.AddEntry(EnrContentKey.Id, id);
        enr.AddEntry(EnrContentKey.Ip, ip);
        enr.AddEntry(EnrContentKey.Secp256K1, pubKey);
        enr.AddEntry(EnrContentKey.Udp, udp);

        Assert.AreEqual(enrString, enr.ToString());
    }

    [Test]
    public void Test_EnrStringSerialization_ShouldSerializeCorrectly()
    {
        var enr = EnrRecordExtensions.FromString(
            "enr:-IS4QHCYrYZbAKWCBRlAy5zzaDZXJBGkcnh4MHcBFZntXNFrdvJjX04jRzjzCBOonrkTfj499SZuOh8R33Ls8RRcy5wBgmlkgnY0gmlwhH8AAAGJc2VjcDI1NmsxoQPKY0yuDUmstAHYpMa2_oxVtw0RW_QAdpzBQA8yWM0xOIN1ZHCCdl8");
        var signature =
            Convert.FromHexString(
                "7098ad865b00a582051940cb9cf36836572411a47278783077011599ed5cd16b76f2635f4e234738f30813a89eb9137e3e3df5266e3a1f11df72ecf1145ccb9c");
        Assert.AreEqual(signature, enr.Signature);
        Assert.AreEqual(1, enr.SequenceNumber);

        var id = enr.GetEntry<EntryId>(EnrContentKey.Id)!.Value;
        Assert.AreEqual("v4", id);

        var ip = enr.GetEntry<EntryIp>(EnrContentKey.Ip)!.Value;
        Assert.AreEqual(new IPAddress(Convert.FromHexString("7f000001")), ip);

        var pubKey = enr.GetEntry<EntrySecp256K1>(EnrContentKey.Secp256K1)!.Value;
        Assert.AreEqual(Convert.FromHexString("03ca634cae0d49acb401d8a4c6b6fe8c55b70d115bf400769cc1400f3258cd3138"),
            pubKey);

        var udp = enr.GetEntry<EntryUdp>(EnrContentKey.Udp)!.Value;
        Assert.AreEqual(Convert.ToInt32("765f", 16), udp);
    }

    [Test]
    public void Test_V4IdentitySchemeSigning_ShouldSignContentCorrectly()
    {
        var enr = EnrRecordExtensions.FromString(
            "enr:-IS4QHCYrYZbAKWCBRlAy5zzaDZXJBGkcnh4MHcBFZntXNFrdvJjX04jRzjzCBOonrkTfj499SZuOh8R33Ls8RRcy5wBgmlkgnY0gmlwhH8AAAGJc2VjcDI1NmsxoQPKY0yuDUmstAHYpMa2_oxVtw0RW_QAdpzBQA8yWM0xOIN1ZHCCdl8");
        var privateKey = Convert.FromHexString("b71c71a67e1177ad4e901695e1b4b9ee17ae16c6668d313eac2f96dbcda3f291");
        var identityScheme = new V4IdentityScheme(privateKey);
        var signature = identityScheme.SignEnrRecord(enr);
        var expectedSignature =
            Convert.FromHexString(
                "7098ad865b00a582051940cb9cf36836572411a47278783077011599ed5cd16b76f2635f4e234738f30813a89eb9137e3e3df5266e3a1f11df72ecf1145ccb9c");
        Assert.AreEqual(expectedSignature, signature);
    }

    [Test]
    public void Test_V4RecordVerification_ShouldVerifySignatureCorrectly()
    {
        var enr = EnrRecordExtensions.FromString(
            "enr:-IS4QHCYrYZbAKWCBRlAy5zzaDZXJBGkcnh4MHcBFZntXNFrdvJjX04jRzjzCBOonrkTfj499SZuOh8R33Ls8RRcy5wBgmlkgnY0gmlwhH8AAAGJc2VjcDI1NmsxoQPKY0yuDUmstAHYpMa2_oxVtw0RW_QAdpzBQA8yWM0xOIN1ZHCCdl8");
        Assert.AreEqual(true, V4IdentityScheme.VerifyEnrRecord(enr));
    }

    [Test]
    public void Test_V4NodeIdentityDerivation_ShouldDeriveNodeIdCorrectly()
    {
        var enr = EnrRecordExtensions.FromString(
            "enr:-IS4QHCYrYZbAKWCBRlAy5zzaDZXJBGkcnh4MHcBFZntXNFrdvJjX04jRzjzCBOonrkTfj499SZuOh8R33Ls8RRcy5wBgmlkgnY0gmlwhH8AAAGJc2VjcDI1NmsxoQPKY0yuDUmstAHYpMa2_oxVtw0RW_QAdpzBQA8yWM0xOIN1ZHCCdl8");
        var nodeId = Convert.FromHexString("a448f24c6d18e575453db13171562b71999873db5b286df957af199ec94617f7");
        Console.WriteLine(string.Join(", ", nodeId));
        Assert.AreEqual(nodeId, V4IdentityScheme.DeriveNodeId(enr));
    }
}