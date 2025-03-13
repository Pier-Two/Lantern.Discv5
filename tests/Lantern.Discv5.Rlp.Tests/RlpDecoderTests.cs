using System.Text;
using NUnit.Framework;

namespace Lantern.Discv5.Rlp.Tests;

[TestFixture]
public class RlpDecoderTests
{
    [Test]
    public void Test_RlpDecoder_ShouldDecodeSingleCharacterCorrectly()
    {
        var rawValue = "a";
        var value = Encoding.UTF8.GetBytes(rawValue);
        var encodedBytes = RlpEncoder.EncodeBytes(value);
        var decodedBytes = RlpDecoder.Decode(encodedBytes);
        Assert.AreEqual(decodedBytes.Length, 1);
        Assert.AreEqual(rawValue, Encoding.UTF8.GetString(decodedBytes[0].GetData()));
    }

    [Test]
    public void Test_RlpDecoder_ShouldDecodeShortStringCorrectly()
    {
        var rawValue = "abc";
        var value = Encoding.UTF8.GetBytes(rawValue);
        var encodedBytes = RlpEncoder.EncodeBytes(value);
        var decodedBytes = RlpDecoder.Decode(encodedBytes);
        Assert.AreEqual(decodedBytes.Length, 1);
        Assert.AreEqual(rawValue, Encoding.UTF8.GetString(decodedBytes[0].GetData()));
    }

    [Test]
    public void Test_RlpDecoder_ShouldDecodeLongStringCorrectly()
    {
        var rawValue = "frefefefrfrsefsfsfsefesfsfsffrefefefrfrsefsfsfsefesfsfsf";
        var value = Encoding.UTF8.GetBytes(rawValue);
        var encodedBytes = RlpEncoder.EncodeBytes(value);
        var decodedBytes = RlpDecoder.Decode(encodedBytes);
        Assert.AreEqual(decodedBytes.Length, 1);
        Assert.AreEqual(rawValue, Encoding.UTF8.GetString(decodedBytes[0].GetData()));
    }

    [Test]
    public void Test_RlpDecoder_ShouldDecodeSmallIntegerCorrectly()
    {
        var rawValue = 23;
        var value = ByteArrayUtils.ToBigEndianBytesTrimmed(rawValue);
        var encodedBytes = RlpEncoder.EncodeBytes(value);
        var decodedBytes = RlpDecoder.Decode(encodedBytes);
        Assert.AreEqual(decodedBytes.Length, 1);
        Assert.AreEqual(rawValue, RlpExtensions.ByteArrayToUInt64(decodedBytes[0].GetData()));
    }

    [Test]
    public void Test_RlpDecoder_ShouldDecodeLargeIntegerCorrectly()
    {
        var rawValue = 999999999;
        var value = ByteArrayUtils.ToBigEndianBytesTrimmed(rawValue);
        var encodedBytes = RlpEncoder.EncodeBytes(value);
        var decodedBytes = RlpDecoder.Decode(encodedBytes);
        Assert.AreEqual(decodedBytes.Length, 1);
        Assert.AreEqual(rawValue, RlpExtensions.ByteArrayToUInt64(decodedBytes[0].GetData()));
    }

    [Test]
    public void Test_RlpDecoder_ShouldDecodeShortCollectionCorrectly()
    {
        var rawValue = new byte[] { 4, 23, 45, 6 };
        var bytes = RlpEncoder.EncodeCollectionOfBytes(rawValue);
        var encodedBytes = RlpDecoder.Decode(bytes);
        Assert.IsTrue(rawValue.SequenceEqual(encodedBytes[0].GetData()));
    }

    [Test]
    public void Test_RlpDecoder_ShouldRecursivelyDecodeShortCollectionCorrectly()
    {
        var rawValue = new byte[] { 4, 23, 45, 6 };
        var bytes = RlpEncoder.EncodeCollectionsOfBytes(rawValue);
        var listsRlp = RlpDecoder.Decode(bytes);
        Assert.AreEqual(listsRlp.Length, 1);
        Assert.IsTrue(rawValue.SequenceEqual(listsRlp[0].GetData()));
    }

    [Test]
    public void Test_RlpDecoder_ShouldRecursivelyDecodeEncodedCollectionCorrectly()
    {
        var rawValue = new byte[] { 4, 23, 45, 6 };
        var bytes = RlpEncoder.EncodeCollectionsOfBytes(rawValue);
        var listsRlp = RlpDecoder.Decode(bytes);
        Assert.AreEqual(listsRlp.Length, 1);
        Assert.IsTrue(rawValue.SequenceEqual(listsRlp[0].GetData()));
    }

    //[Test]
    //public void Test_RlpDecoder_ShouldDecodeLargeCollectionCorrectly()
    //{
    //    var bytes = new byte[]
    //    {
    //        248, 132, 184, 64, 112, 152, 173, 134, 91, 0, 165, 130, 5, 25, 64, 203, 156, 243, 104, 54, 87, 36, 17, 164,
    //        114, 120, 120, 48, 119, 1, 21, 153, 237, 92, 209, 107, 118, 242, 99, 95, 78, 35, 71, 56, 243, 8, 19, 168,
    //        158, 185, 19, 126, 62, 61, 245, 38, 110, 58, 31, 17, 223, 114, 236, 241, 20, 92, 203, 156, 1, 130, 105, 100,
    //        130, 118, 52, 130, 105, 112, 132, 127, 0, 0, 1, 137, 115, 101, 99, 112, 50, 53, 54, 107, 49, 161, 3, 202,
    //        99, 76, 174, 13, 73, 172, 180, 1, 216, 164, 198, 182, 254, 140, 85, 183, 13, 17, 91, 244, 0, 118, 156, 193,
    //        64, 15, 50, 88, 205, 49, 56, 131, 117, 100, 112, 130, 118, 95
    //    };
    //    var expectedBytes = new byte[]
    //    {
    //        112, 152, 173, 134, 91, 0, 165, 130, 5, 25, 64, 203, 156, 243, 104, 54, 87, 36, 17, 164, 114, 120, 120, 48,
    //        119, 1, 21, 153, 237, 92, 209, 107, 118, 242, 99, 95, 78, 35, 71, 56, 243, 8, 19, 168, 158, 185, 19, 126,
    //        62, 61, 245, 38, 110, 58, 31, 17, 223, 114, 236, 241, 20, 92, 203, 156, 1, 105, 100, 118, 52, 105, 112, 127,
    //        0, 0, 1, 115, 101, 99, 112, 50, 53, 54, 107, 49, 3, 202, 99, 76, 174, 13, 73, 172, 180, 1, 216, 164, 198,
    //        182, 254, 140, 85, 183, 13, 17, 91, 244, 0, 118, 156, 193, 64, 15, 50, 88, 205, 49, 56, 117, 100, 112, 118,
    //        95
    //    };
    //    var encodedBytes = RlpDecoder.Decode(bytes);
    //    Assert.IsTrue(expectedBytes.SequenceEqual(encodedBytes.SelectMany(e => e.ToArray()).ToArray()));
    //}

    //[Test]
    //public void Test_RlpDecoder_2ShouldDecodeLargeCollectionCorrectly()
    //{
    //    var bytes = new byte[]
    //    {
    //        249, 1, 81, 136, 52, 75, 177, 111, 250, 100, 248, 30, 2, 249, 1, 68, 248, 132, 184, 64, 112, 152, 173, 134,
    //        91, 0, 165, 130, 5, 25, 64, 203, 156, 243, 104, 54, 87, 36, 17, 164, 114, 120, 120, 48, 119, 1, 21, 153,
    //        237, 92, 209, 107, 118, 242, 99, 95, 78, 35, 71, 56, 243, 8, 19, 168, 158, 185, 19, 126, 62, 61, 245, 38,
    //        110, 58, 31, 17, 223, 114, 236, 241, 20, 92, 203, 156, 1, 130, 105, 100, 130, 118, 52, 130, 105, 112, 132,
    //        127, 0, 0, 1, 137, 115, 101, 99, 112, 50, 53, 54, 107, 49, 161, 3, 202, 99, 76, 174, 13, 73, 172, 180, 1,
    //        216, 164, 198, 182, 254, 140, 85, 183, 13, 17, 91, 244, 0, 118, 156, 193, 64, 15, 50, 88, 205, 49, 56, 131,
    //        117, 100, 112, 130, 118, 95, 248, 188, 184, 64, 228, 180, 210, 27, 207, 13, 215, 68, 112, 42, 112, 3, 87,
    //        12, 202, 69, 141, 116, 149, 10, 231, 64, 35, 109, 24, 27, 45, 159, 69, 44, 252, 129, 107, 191, 34, 208, 220,
    //        44, 65, 146, 210, 87, 174, 222, 150, 146, 84, 239, 82, 245, 62, 223, 114, 169, 89, 132, 212, 46, 151, 121,
    //        34, 62, 45, 95, 1, 135, 97, 116, 116, 110, 101, 116, 115, 136, 0, 0, 0, 0, 0, 0, 0, 0, 132, 101, 116, 104,
    //        50, 144, 238, 40, 215, 179, 0, 0, 0, 114, 70, 5, 0, 0, 0, 0, 0, 0, 130, 105, 100, 130, 118, 52, 130, 105,
    //        112, 132, 64, 225, 78, 1, 137, 115, 101, 99, 112, 50, 53, 54, 107, 49, 161, 2, 32, 49, 67, 5, 188, 145, 165,
    //        175, 199, 72, 213, 49, 16, 203, 226, 187, 242, 237, 147, 36, 77, 171, 90, 246, 161, 246, 113, 170, 45, 0,
    //        132, 26, 136, 115, 121, 110, 99, 110, 101, 116, 115, 0, 131, 116, 99, 112, 130, 35, 40, 131, 117, 100, 112,
    //        130, 35, 40
    //    };
    //    var expectedBytes = new byte[]
    //    {
    //        52, 75, 177, 111, 250, 100, 248, 30, 2, 112, 152, 173, 134, 91, 0, 165, 130, 5, 25, 64, 203, 156, 243, 104,
    //        54, 87, 36, 17, 164, 114, 120, 120, 48, 119, 1, 21, 153, 237, 92, 209, 107, 118, 242, 99, 95, 78, 35, 71,
    //        56, 243, 8, 19, 168, 158, 185, 19, 126, 62, 61, 245, 38, 110, 58, 31, 17, 223, 114, 236, 241, 20, 92, 203,
    //        156, 1, 105, 100, 118, 52, 105, 112, 127, 0, 0, 1, 115, 101, 99, 112, 50, 53, 54, 107, 49, 3, 202, 99, 76,
    //        174, 13, 73, 172, 180, 1, 216, 164, 198, 182, 254, 140, 85, 183, 13, 17, 91, 244, 0, 118, 156, 193, 64, 15,
    //        50, 88, 205, 49, 56, 117, 100, 112, 118, 95, 228, 180, 210, 27, 207, 13, 215, 68, 112, 42, 112, 3, 87, 12,
    //        202, 69, 141, 116, 149, 10, 231, 64, 35, 109, 24, 27, 45, 159, 69, 44, 252, 129, 107, 191, 34, 208, 220, 44,
    //        65, 146, 210, 87, 174, 222, 150, 146, 84, 239, 82, 245, 62, 223, 114, 169, 89, 132, 212, 46, 151, 121, 34,
    //        62, 45, 95, 1, 97, 116, 116, 110, 101, 116, 115, 0, 0, 0, 0, 0, 0, 0, 0, 101, 116, 104, 50, 238, 40, 215,
    //        179, 0, 0, 0, 114, 70, 5, 0, 0, 0, 0, 0, 0, 105, 100, 118, 52, 105, 112, 64, 225, 78, 1, 115, 101, 99, 112,
    //        50, 53, 54, 107, 49, 2, 32, 49, 67, 5, 188, 145, 165, 175, 199, 72, 213, 49, 16, 203, 226, 187, 242, 237,
    //        147, 36, 77, 171, 90, 246, 161, 246, 113, 170, 45, 0, 132, 26, 115, 121, 110, 99, 110, 101, 116, 115, 0,
    //        116, 99, 112, 35, 40, 117, 100, 112, 35, 40
    //    };
    //    var encodedBytes = RlpDecoder.Decode(bytes);
    //    Assert.IsTrue(expectedBytes.SequenceEqual(encodedBytes.SelectMany(e => e.ToArray()).ToArray()));
    //}


    //[Test]
    //public void Test_RlpDecoder_ShouldDecodeEnrWithList()
    //{
    //    var rawValue = "f8b188b47225c9674f99d306f8a5f8a3b840df79b4ffe6fe452449180234ff8e1509237b6decc6d054dc34cbba5f7dcf036a3f067a213dc9944aa5c761ffe450fc9ad032a050a2fb4467198bc3f0d6ac0eb68601926acf868083657468c7c6849b192ad080826964827634826970843b1fcea189736563703235366b31a102ee59efb761353ad2e7458f2e127cb110a5571beeff0d25e95de840331e5725ce84736e6170c08374637082765f8375647082765f";
    //    var value = Convert.FromHexString(rawValue);
    //    var enrArray = RlpDecoder.Decode(value);
    //    var enrsRlps = RlpDecoder.Decode(enrArray[2].InnerSpan).ToArray();
    //    var enrs = enrsRlps.Select(b => RlpDecoder.Decode(b.InnerSpan)).ToList();

    //    Assert.AreEqual(enrs.Length, 1);
    //    Assert.AreEqual(enrs[0].Length, 16);
    //}

    //[Test]
    //public void Test_RlpDecoder_ShouldDecodeWithoutFlatteningB()
    //{
    //    var rawValue = "f9015788b47225c9674f99d306f9014af8a3b840df79b4ffe6fe452449180234ff8e1509237b6decc6d054dc34cbba5f7dcf036a3f067a213dc9944aa5c761ffe450fc9ad032a050a2fb4467198bc3f0d6ac0eb68601926acf868083657468c7c6849b192ad080826964827634826970843b1fcea189736563703235366b31a102ee59efb761353ad2e7458f2e127cb110a5571beeff0d25e95de840331e5725ce84736e6170c08374637082765f8375647082765ff8a3b840df79b4ffe6fe452449180234ff8e1509237b6decc6d054dc34cbba5f7dcf036a3f067a213dc9944aa5c761ffe450fc9ad032a050a2fb4467198bc3f0d6ac0eb68601926acf868083657468c7c6849b192ad080826964827634826970843b1fcea189736563703235366b31a102ee59efb761353ad2e7458f2e127cb110a5571beeff0d25e95de840331e5725ce84736e6170c08374637082765f8375647082765f";
    //    var value = Convert.FromHexString(rawValue);
    //    var enrArray = RlpDecoder.Decode(value);
    //    var enrsRlps = RlpDecoder.Decode(enrArray[2].InnerSpan).ToArray();
    //    var enrs = enrsRlps.Select(b => RlpDecoder.Decode(b.InnerSpan)).ToList();

    //    Assert.AreEqual(enrs.Length, 2);
    //    Assert.AreEqual(enrs[0].Length, 16);
    //    Assert.AreEqual(enrs[1].Length, 16);
    //}
}