using System.Net;
using Lantern.Discv5.Enr.Content;
using Lantern.Discv5.Enr.Content.EntryTypes;
using Lantern.Discv5.Enr.Identity;
using NUnit.Framework;

namespace Lantern.Discv5.Enr.Tests;

[TestFixture]
public class EnrTests
{
    [Test]
    public void Test_ConvertEnrRecordToEnrString_ShouldConvertCorrectly()
    {
        var enrString =
            "enr:-IS4QHCYrYZbAKWCBRlAy5zzaDZXJBGkcnh4MHcBFZntXNFrdvJjX04jRzjzCBOonrkTfj499SZuOh8R33Ls8RRcy5wBgmlkgnY0gmlwhH8AAAGJc2VjcDI1NmsxoQPKY0yuDUmstAHYpMa2_oxVtw0RW_QAdpzBQA8yWM0xOIN1ZHCCdl8";
        var signature =
            Convert.FromHexString(
                "7098ad865b00a582051940cb9cf36836572411a47278783077011599ed5cd16b76f2635f4e234738f30813a89eb9137e3e3df5266e3a1f11df72ecf1145ccb9c");

        var enr = new EnrRecord(signature);
        var id = new EntryId("v4");
        var ip = new EntryIp(new IPAddress(Convert.FromHexString("7f000001")));
        var pubKey =
            new EntrySecp256K1(
                Convert.FromHexString("03ca634cae0d49acb401d8a4c6b6fe8c55b70d115bf400769cc1400f3258cd3138"));
        var udp = new EntryUdp(30303);
        
        enr.SequenceNumber = 1;
        enr.SetEntry(EnrContentKey.Id, id);
        enr.SetEntry(EnrContentKey.Ip, ip);
        enr.SetEntry(EnrContentKey.Secp256K1, pubKey);
        enr.SetEntry(EnrContentKey.Udp, udp);
        Assert.AreEqual(enrString, enr.ToString());
    }

    [Test]
    public void Test_EnrStringSerialization_ShouldDecodeCorrectly()
    {
        var enrString =
            "enr:-IS4QHCYrYZbAKWCBRlAy5zzaDZXJBGkcnh4MHcBFZntXNFrdvJjX04jRzjzCBOonrkTfj499SZuOh8R33Ls8RRcy5wBgmlkgnY0gmlwhH8AAAGJc2VjcDI1NmsxoQPKY0yuDUmstAHYpMa2_oxVtw0RW_QAdpzBQA8yWM0xOIN1ZHCCdl8";
        var enr = new EnrRecordFactory().CreateFromString(enrString);
        var signature =
            Convert.FromHexString(
                "7098ad865b00a582051940cb9cf36836572411a47278783077011599ed5cd16b76f2635f4e234738f30813a89eb9137e3e3df5266e3a1f11df72ecf1145ccb9c");

        Assert.AreEqual(signature, enr.Signature);
        Assert.AreEqual(1, enr.SequenceNumber);

        var id = enr.GetEntry<EntryId>(EnrContentKey.Id).Value;
        Assert.AreEqual("v4", id);

        var ip = enr.GetEntry<EntryIp>(EnrContentKey.Ip)!.Value;
        Assert.AreEqual(new IPAddress(Convert.FromHexString("7f000001")), ip);

        var pubKey = enr.GetEntry<EntrySecp256K1>(EnrContentKey.Secp256K1)!.Value;
        Assert.AreEqual(Convert.FromHexString("03ca634cae0d49acb401d8a4c6b6fe8c55b70d115bf400769cc1400f3258cd3138"),
            pubKey);

        var udp = enr.GetEntry<EntryUdp>(EnrContentKey.Udp)!.Value;
        Assert.AreEqual(Convert.ToInt32("765f", 16), udp);
        Assert.AreEqual(enrString, enr.ToString());
    }

    [Test]
    public void Test_EnrStringSerialization_ShouldDecodeEth2EnrCorrectly()
    {
        var enrString =
            "enr:-Ly4QOS00hvPDddEcCpwA1cMykWNdJUK50AjbRgbLZ9FLPyBa78i0NwsQZLSV67elpJU71L1Pt9yqVmE1C6XeSI-LV8Bh2F0dG5ldHOIAAAAAAAAAACEZXRoMpDuKNezAAAAckYFAAAAAAAAgmlkgnY0gmlwhEDhTgGJc2VjcDI1NmsxoQIgMUMFvJGlr8dI1TEQy-K78u2TJE2rWvah9nGqLQCEGohzeW5jbmV0cwCDdGNwgiMog3VkcIIjKA";

        var enr = new EnrRecordFactory().CreateFromString(enrString);
        var signature =
            Convert.FromHexString(
                "e4b4d21bcf0dd744702a7003570cca458d74950ae740236d181b2d9f452cfc816bbf22d0dc2c4192d257aede969254ef52f53edf72a95984d42e9779223e2d5f");

        Assert.AreEqual(signature, enr.Signature);
        Assert.AreEqual(1, enr.SequenceNumber);

        var attnets = enr.GetEntry<EntryAttnets>(EnrContentKey.Attnets).Value;
        Assert.AreEqual(Convert.FromHexString("0000000000000000"), attnets);

        var eth2 = enr.GetEntry<EntryEth2>(EnrContentKey.Eth2).Value;
        Assert.AreEqual(Convert.FromHexString("ee28d7b3000000724605000000000000"), eth2);

        var id = enr.GetEntry<EntryId>(EnrContentKey.Id).Value;
        Assert.AreEqual("v4", id);

        var ip = enr.GetEntry<EntryIp>(EnrContentKey.Ip).Value;
        Assert.AreEqual(new IPAddress(Convert.FromHexString("40E14E01")), ip);

        var pubKey = enr.GetEntry<EntrySecp256K1>(EnrContentKey.Secp256K1).Value;
        Assert.AreEqual(Convert.FromHexString("0220314305bc91a5afc748d53110cbe2bbf2ed93244dab5af6a1f671aa2d00841a"),
            pubKey);

        var udp = enr.GetEntry<EntryUdp>(EnrContentKey.Udp).Value;
        Assert.AreEqual(Convert.ToInt32("2328", 16), udp);
        Assert.AreEqual(enrString, enr.ToString());
    }

    [Test]
    public void Test_V4IdentitySchemeSigning_ShouldSignContentCorrectly()
    {
        var enr = new EnrRecordFactory().CreateFromString(
            "enr:-IS4QHCYrYZbAKWCBRlAy5zzaDZXJBGkcnh4MHcBFZntXNFrdvJjX04jRzjzCBOonrkTfj499SZuOh8R33Ls8RRcy5wBgmlkgnY0gmlwhH8AAAGJc2VjcDI1NmsxoQPKY0yuDUmstAHYpMa2_oxVtw0RW_QAdpzBQA8yWM0xOIN1ZHCCdl8");
        var privateKey = Convert.FromHexString("b71c71a67e1177ad4e901695e1b4b9ee17ae16c6668d313eac2f96dbcda3f291");
        var identityScheme = new IdentitySchemeV4Signer(privateKey);
        var signature = identityScheme.SignRecord(enr);
        var expectedSignature =
            Convert.FromHexString(
                "7098ad865b00a582051940cb9cf36836572411a47278783077011599ed5cd16b76f2635f4e234738f30813a89eb9137e3e3df5266e3a1f11df72ecf1145ccb9c");
        Assert.AreEqual(expectedSignature, signature);
    }

    [Test]
    public void Test_V4RecordVerification_ShouldVerifySignatureCorrectly()
    {
        var enr = new EnrRecordFactory().CreateFromString(
            "enr:-IS4QHCYrYZbAKWCBRlAy5zzaDZXJBGkcnh4MHcBFZntXNFrdvJjX04jRzjzCBOonrkTfj499SZuOh8R33Ls8RRcy5wBgmlkgnY0gmlwhH8AAAGJc2VjcDI1NmsxoQPKY0yuDUmstAHYpMa2_oxVtw0RW_QAdpzBQA8yWM0xOIN1ZHCCdl8");
        var identityVerifier = new IdentitySchemeV4Verifier();
        Assert.AreEqual(true, identityVerifier.VerifyRecord(enr));
    }

    [Test]
    public void Test_V4NodeIdentityDerivation_ShouldDeriveNodeIdCorrectly()
    {
        var enr = new EnrRecordFactory().CreateFromString(
            "enr:-IS4QHCYrYZbAKWCBRlAy5zzaDZXJBGkcnh4MHcBFZntXNFrdvJjX04jRzjzCBOonrkTfj499SZuOh8R33Ls8RRcy5wBgmlkgnY0gmlwhH8AAAGJc2VjcDI1NmsxoQPKY0yuDUmstAHYpMa2_oxVtw0RW_QAdpzBQA8yWM0xOIN1ZHCCdl8");
        var nodeId = Convert.FromHexString("a448f24c6d18e575453db13171562b71999873db5b286df957af199ec94617f7");
        var identityVerifier = new IdentitySchemeV4Verifier();
        Assert.AreEqual(nodeId, identityVerifier.GetNodeIdFromRecord(enr));
    }
}