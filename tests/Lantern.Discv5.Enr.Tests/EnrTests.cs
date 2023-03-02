using System.Net;
using Lantern.Discv5.Enr.EntryType;
using Lantern.Discv5.Rlp;
using NeoSmart.Utils;
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
    public void Test_EnrStringSerialization_ShouldDecodeCorrectly()
    {
        var enrString =
            "enr:-IS4QHCYrYZbAKWCBRlAy5zzaDZXJBGkcnh4MHcBFZntXNFrdvJjX04jRzjzCBOonrkTfj499SZuOh8R33Ls8RRcy5wBgmlkgnY0gmlwhH8AAAGJc2VjcDI1NmsxoQPKY0yuDUmstAHYpMa2_oxVtw0RW_QAdpzBQA8yWM0xOIN1ZHCCdl8";

        var enr = EnrRecordExtensions.FromString(enrString);
        var signature =
            Convert.FromHexString(
                "7098ad865b00a582051940cb9cf36836572411a47278783077011599ed5cd16b76f2635f4e234738f30813a89eb9137e3e3df5266e3a1f11df72ecf1145ccb9c");

        Assert.AreEqual(signature,enr.Signature);
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

        var enr = EnrRecordExtensions.FromString(enrString);
        var signature =
            Convert.FromHexString(
                "e4b4d21bcf0dd744702a7003570cca458d74950ae740236d181b2d9f452cfc816bbf22d0dc2c4192d257aede969254ef52f53edf72a95984d42e9779223e2d5f");

        Assert.AreEqual(signature,enr.Signature);
        Assert.AreEqual(1, enr.SequenceNumber);
        
        var attnets = enr.GetEntry<EntryAttnets>(EnrContentKey.Attnets).Value;
        Assert.AreEqual(Convert.FromHexString("0000000000000000") , attnets);
        
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
        Assert.AreEqual(nodeId, V4IdentityScheme.DeriveNodeId(enr));
    }

    [Test]
    public void Test()
    {
        var bytes = new byte[] { 249, 1, 81, 136, 238, 105, 15, 189, 89, 78, 17, 171, 2, 249, 1, 68, 248, 132, 184, 64, 112, 152, 173, 134, 91, 0, 165, 130, 5, 25, 64, 203, 156, 243, 104, 54, 87, 36, 17, 164, 114, 120, 120, 48, 119, 1, 21, 153, 237, 92, 209, 107, 118, 242, 99, 95, 78, 35, 71, 56, 243, 8, 19, 168, 158, 185, 19, 126, 62, 61, 245, 38, 110, 58, 31, 17, 223, 114, 236, 241, 20, 92, 203, 156, 1, 130, 105, 100, 130, 118, 52, 130, 105, 112, 132, 127, 0, 0, 1, 137, 115, 101, 99, 112, 50, 53, 54, 107, 49, 161, 3, 202, 99, 76, 174, 13, 73, 172, 180, 1, 216, 164, 198, 182, 254, 140, 85, 183, 13, 17, 91, 244, 0, 118, 156, 193, 64, 15, 50, 88, 205, 49, 56, 131, 117, 100, 112, 130, 118, 95, 248, 188, 184, 64, 228, 180, 210, 27, 207, 13, 215, 68, 112, 42, 112, 3, 87, 12, 202, 69, 141, 116, 149, 10, 231, 64, 35, 109, 24, 27, 45, 159, 69, 44, 252, 129, 107, 191, 34, 208, 220, 44, 65, 146, 210, 87, 174, 222, 150, 146, 84, 239, 82, 245, 62, 223, 114, 169, 89, 132, 212, 46, 151, 121, 34, 62, 45, 95, 1, 135, 97, 116, 116, 110, 101, 116, 115, 136, 0, 0, 0, 0, 0, 0, 0, 0, 132, 101, 116, 104, 50, 144, 238, 40, 215, 179, 0, 0, 0, 114, 70, 5, 0, 0, 0, 0, 0, 0, 130, 105, 100, 130, 118, 52, 130, 105, 112, 132, 64, 225, 78, 1, 137, 115, 101, 99, 112, 50, 53, 54, 107, 49, 161, 2, 32, 49, 67, 5, 188, 145, 165, 175, 199, 72, 213, 49, 16, 203, 226, 187, 242, 237, 147, 36, 77, 171, 90, 246, 161, 246, 113, 170, 45, 0, 132, 26, 136, 115, 121, 110, 99, 110, 101, 116, 115, 0, 131, 116, 99, 112, 130, 35, 40, 131, 117, 100, 112, 130, 35, 40 };
        var decodedItems = RlpDecoder.Decode(bytes);
        
        foreach (var item in decodedItems)
        {
            Console.WriteLine(string.Join(", ", item));
        }
    }
}