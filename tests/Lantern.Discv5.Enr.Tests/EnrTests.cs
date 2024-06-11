﻿using System.Net;
using Lantern.Discv5.Enr.Entries;
using Lantern.Discv5.Enr.Identity.V4;
using Multiformats.Base;
using Multiformats.Hash;
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

        var id = new EntryId("v4");
        var ip = new EntryIp(new IPAddress(Convert.FromHexString("7f000001")));
        var pubKey =
            new EntrySecp256K1(
                Convert.FromHexString("03ca634cae0d49acb401d8a4c6b6fe8c55b70d115bf400769cc1400f3258cd3138"));
        var udp = new EntryUdp(30303);
        var entries = new Dictionary<string, IEntry>
        {
            { EnrEntryKey.Id, id },
            { EnrEntryKey.Ip, ip },
            { EnrEntryKey.Secp256K1, pubKey },
            { EnrEntryKey.Udp, udp }
        };

        var enr = new Enr(entries, new IdentityVerifierV4(), null, signature);

        Assert.AreEqual(enrString, enr.ToString());
    }

    [Test]
    public void Test_EnrStringSerialization_ShouldDecodeCorrectly()
    {
        var enrString =
            "enr:-IS4QHCYrYZbAKWCBRlAy5zzaDZXJBGkcnh4MHcBFZntXNFrdvJjX04jRzjzCBOonrkTfj499SZuOh8R33Ls8RRcy5wBgmlkgnY0gmlwhH8AAAGJc2VjcDI1NmsxoQPKY0yuDUmstAHYpMa2_oxVtw0RW_QAdpzBQA8yWM0xOIN1ZHCCdl8";
        var enrEntryRegistry = new EnrEntryRegistry();
        var enr = new EnrFactory(enrEntryRegistry).CreateFromString(enrString, new IdentityVerifierV4());
        var signature =
            Convert.FromHexString(
                "7098ad865b00a582051940cb9cf36836572411a47278783077011599ed5cd16b76f2635f4e234738f30813a89eb9137e3e3df5266e3a1f11df72ecf1145ccb9c");

        Assert.AreEqual(signature, enr.Signature);
        Assert.AreEqual(1, enr.SequenceNumber);

        var id = enr.GetEntry<EntryId>(EnrEntryKey.Id).Value;
        Assert.AreEqual("v4", id);

        var ip = enr.GetEntry<EntryIp>(EnrEntryKey.Ip)!.Value;
        Assert.AreEqual(new IPAddress(Convert.FromHexString("7f000001")), ip);

        var pubKey = enr.GetEntry<EntrySecp256K1>(EnrEntryKey.Secp256K1)!.Value;
        Assert.AreEqual(Convert.FromHexString("03ca634cae0d49acb401d8a4c6b6fe8c55b70d115bf400769cc1400f3258cd3138"),
            pubKey);

        var udp = enr.GetEntry<EntryUdp>(EnrEntryKey.Udp)!.Value;
        Assert.AreEqual(Convert.ToInt32("765f", 16), udp);
        Assert.AreEqual(enrString, enr.ToString());
    }

    [Test]
    public void Test_EnrStringSerialization_ShouldDecodeEth2EnrCorrectly()
    {
        var enrString =
            "enr:-Ly4QOS00hvPDddEcCpwA1cMykWNdJUK50AjbRgbLZ9FLPyBa78i0NwsQZLSV67elpJU71L1Pt9yqVmE1C6XeSI-LV8Bh2F0dG5ldHOIAAAAAAAAAACEZXRoMpDuKNezAAAAckYFAAAAAAAAgmlkgnY0gmlwhEDhTgGJc2VjcDI1NmsxoQIgMUMFvJGlr8dI1TEQy-K78u2TJE2rWvah9nGqLQCEGohzeW5jbmV0cwCDdGNwgiMog3VkcIIjKA";
        var enrEntryRegistry = new EnrEntryRegistry();
        var enr = new EnrFactory(enrEntryRegistry).CreateFromString(enrString, new IdentityVerifierV4());
        var signature =
            Convert.FromHexString(
                "e4b4d21bcf0dd744702a7003570cca458d74950ae740236d181b2d9f452cfc816bbf22d0dc2c4192d257aede969254ef52f53edf72a95984d42e9779223e2d5f");

        Assert.AreEqual(signature, enr.Signature);
        Assert.AreEqual(1, enr.SequenceNumber);

        var attnets = enr.GetEntry<EntryAttnets>(EnrEntryKey.Attnets).Value;
        Assert.AreEqual(Convert.FromHexString("0000000000000000"), attnets);

        var syncnets = enr.GetEntry<EntrySyncnets>(EnrEntryKey.Syncnets).Value;
        Assert.AreEqual(Convert.FromHexString("00"), syncnets);

        var eth2 = enr.GetEntry<EntryEth2>(EnrEntryKey.Eth2).Value;
        Assert.AreEqual(Convert.FromHexString("ee28d7b3000000724605000000000000"), eth2);

        var id = enr.GetEntry<EntryId>(EnrEntryKey.Id).Value;
        Assert.AreEqual("v4", id);

        var ip = enr.GetEntry<EntryIp>(EnrEntryKey.Ip).Value;
        Assert.AreEqual(new IPAddress(Convert.FromHexString("40E14E01")), ip);

        var pubKey = enr.GetEntry<EntrySecp256K1>(EnrEntryKey.Secp256K1).Value;
        Assert.AreEqual(Convert.FromHexString("0220314305bc91a5afc748d53110cbe2bbf2ed93244dab5af6a1f671aa2d00841a"),
            pubKey);

        var udp = enr.GetEntry<EntryUdp>(EnrEntryKey.Udp).Value;
        Assert.AreEqual(Convert.ToInt32("2328", 16), udp);

        Assert.AreEqual(enrString, enr.ToString());
    }

    [Test]
    public void Test_V4IdentitySchemeSigning_ShouldSignContentCorrectly()
    {
        var enrEntryRegistry = new EnrEntryRegistry();
        var enr = new EnrFactory(enrEntryRegistry).CreateFromString(
            "enr:-IS4QHCYrYZbAKWCBRlAy5zzaDZXJBGkcnh4MHcBFZntXNFrdvJjX04jRzjzCBOonrkTfj499SZuOh8R33Ls8RRcy5wBgmlkgnY0gmlwhH8AAAGJc2VjcDI1NmsxoQPKY0yuDUmstAHYpMa2_oxVtw0RW_QAdpzBQA8yWM0xOIN1ZHCCdl8", new IdentityVerifierV4());
        var privateKey = Convert.FromHexString("b71c71a67e1177ad4e901695e1b4b9ee17ae16c6668d313eac2f96dbcda3f291");
        var identityScheme = new IdentitySignerV4(privateKey);
        var signature = identityScheme.SignRecord(enr);
        var expectedSignature =
            Convert.FromHexString(
                "7098ad865b00a582051940cb9cf36836572411a47278783077011599ed5cd16b76f2635f4e234738f30813a89eb9137e3e3df5266e3a1f11df72ecf1145ccb9c");
        Assert.AreEqual(expectedSignature, signature);
    }

    [Test]
    public void Test_V4RecordVerification_ShouldVerifySignatureCorrectly()
    {
        var enrEntryRegistry = new EnrEntryRegistry();
        var enr = new EnrFactory(enrEntryRegistry).CreateFromString(
            "enr:-IS4QHCYrYZbAKWCBRlAy5zzaDZXJBGkcnh4MHcBFZntXNFrdvJjX04jRzjzCBOonrkTfj499SZuOh8R33Ls8RRcy5wBgmlkgnY0gmlwhH8AAAGJc2VjcDI1NmsxoQPKY0yuDUmstAHYpMa2_oxVtw0RW_QAdpzBQA8yWM0xOIN1ZHCCdl8", new IdentityVerifierV4());
        var identityVerifier = new IdentityVerifierV4();
        Assert.AreEqual(true, identityVerifier.VerifyRecord(enr));
    }

    [Test]
    public void Test_V4NodeIdentityDerivation_ShouldDeriveNodeIdCorrectly()
    {
        var enrEntryRegistry = new EnrEntryRegistry();
        var enr = new EnrFactory(enrEntryRegistry).CreateFromString(
            "enr:-IS4QHCYrYZbAKWCBRlAy5zzaDZXJBGkcnh4MHcBFZntXNFrdvJjX04jRzjzCBOonrkTfj499SZuOh8R33Ls8RRcy5wBgmlkgnY0gmlwhH8AAAGJc2VjcDI1NmsxoQPKY0yuDUmstAHYpMa2_oxVtw0RW_QAdpzBQA8yWM0xOIN1ZHCCdl8", new IdentityVerifierV4());
        var nodeId = Convert.FromHexString("a448f24c6d18e575453db13171562b71999873db5b286df957af199ec94617f7");
        var identityVerifier = new IdentityVerifierV4();
        Assert.AreEqual(nodeId, identityVerifier.GetNodeIdFromRecord(enr));
    }

    [Test]
    public void Test_ToEnode_ShouldDeriveEnodeCorrectly()
    {
        var enrEntryRegistry = new EnrEntryRegistry();
        var enr = new EnrFactory(enrEntryRegistry).CreateFromString(
            "enr:-Mq4QLyFLj2R0kwCmxNgO02F2JqHOUAT9CnqK9qHBwJWPlvNR36e9YydkUzFM69E0dzX7hrpOUAJVKsBLb3PysSz-IiGAY7D6Sg4h2F0dG5ldHOIAAAAAAAAAAaEZXRoMpBqlaGpBAAAAP__________gmlkgnY0gmlwhCJkw5SJc2VjcDI1NmsxoQMc6eWKtIsR4Ref474zOEeRKEuHzxrK_jffZrkzzYSuUYhzeW5jbmV0cwCDdGNwgjLIg3VkcILLBIR1ZHA2gi7g", new IdentityVerifierV4());
        var enode = "enode://bc852e3d91d24c029b13603b4d85d89a87394013f429ea2bda870702563e5bcd477e9ef58c9d914cc533af44d1dcd7ee1ae939400954ab012dbdcfcac4b3f888@34.100.195.148:13000?discport=51972";

        Assert.AreEqual(enode, enr.ToEnode());
    }

    [Test]
    public void Test_V4PeerIdGeneration_ShouldDerivePeerIdCorrectly()
    {
        var enrEntryRegistry1 = new EnrEntryRegistry();
        var enr = new EnrFactory(enrEntryRegistry1).CreateFromString(
            "enr:-IS4QHCYrYZbAKWCBRlAy5zzaDZXJBGkcnh4MHcBFZntXNFrdvJjX04jRzjzCBOonrkTfj499SZuOh8R33Ls8RRcy5wBgmlkgnY0gmlwhH8AAAGJc2VjcDI1NmsxoQPKY0yuDUmstAHYpMa2_oxVtw0RW_QAdpzBQA8yWM0xOIN1ZHCCdl8", new IdentityVerifierV4());
        var peerId = "16Uiu2HAmSH2XVgZqYHWucap5kuPzLnt2TsNQkoppVxB5eJGvaXwm";

        Assert.AreEqual(peerId, enr.ToPeerId());

        var enrEntryRegistry2 = new EnrEntryRegistry();
        var enr2 = new EnrFactory(enrEntryRegistry2).CreateFromString("enr:-Ku4QImhMc1z8yCiNJ1TyUxdcfNucje3BGwEHzodEZUan8PherEo4sF7pPHPSIB1NNuSg5fZy7qFsjmUKs2ea1Whi0EBh2F0dG5ldHOIAAAAAAAAAACEZXRoMpD1pf1CAAAAAP__________gmlkgnY0gmlwhBLf22SJc2VjcDI1NmsxoQOVphkDqal4QzPMksc5wnpuC3gvSC8AfbFOnZY_On34wIN1ZHCCIyg", new IdentityVerifierV4());
        Console.WriteLine(enr2.ToPeerId());
    }
}