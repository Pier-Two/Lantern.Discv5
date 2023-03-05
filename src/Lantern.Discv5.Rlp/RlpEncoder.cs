using System.Text;

namespace Lantern.Discv5.Rlp;

public static class RlpEncoder
{
    public static byte[] EncodeInteger(int value)
    {
        return Encode(Helpers.ToBigEndianBytes(value), false);
    }

    public static byte[] EncodeUlong(ulong value)
    {
        return Encode(Helpers.ToBigEndianBytes(value), false);
    }

    public static byte[] EncodeHexString(string value)
    {
        if (Convert.FromHexString(value)[0] == Constants.ZeroByte) return new byte[] { 0 };

        return Encode(Convert.FromHexString(value), false);
    }

    public static byte[] EncodeString(string value, Encoding encoding)
    {
        if (!Equals(encoding, Encoding.UTF8) && !Equals(encoding, Encoding.ASCII))
            throw new ArgumentException("Encoding not supported", nameof(encoding));

        return Encode(encoding.GetBytes(value), false);
    }

    public static byte[] EncodeStringCollection(IEnumerable<string> values, Encoding encoding)
    {
        if (!Equals(encoding, Encoding.UTF8) && !Equals(encoding, Encoding.ASCII))
            throw new ArgumentException("Encoding not supported", nameof(encoding));

        using var stream = new MemoryStream();

        foreach (var t in values)
            stream.Write(EncodeString(t, encoding).ToArray());

        return Encode(stream.ToArray(), true);
    }

    public static byte[] EncodeBytes(IEnumerable<byte> item)
    {
        return Encode(item.ToArray(), false);
    }

    public static byte[] EncodeEachByteInBytes(IEnumerable<byte> items)
    {
        using var stream = new MemoryStream();

        foreach (var item in items)
        {
            var encoded = Encode(new[] { item }, false);
            stream.Write(encoded);
        }

        return Encode(stream.ToArray(), true);
    }

    public static byte[] EncodeCollectionOfBytes(IEnumerable<byte> item)
    {
        return Encode(item.ToArray(), true);
    }

    public static byte[] EncodeCollectionsOfBytes(params byte[][] items)
    {
        using var stream = new MemoryStream();

        foreach (var item in items) stream.Write(EncodeCollectionOfBytes(item).ToArray());

        return Encode(stream.ToArray(), true);
    }

    private static byte[] Encode(byte[] array, bool isCollection)
    {
        if (!isCollection) return GetPrefix(array, Constants.ShortItemOffset, Constants.LargeItemOffset);

        return GetPrefix(array, Constants.ShortCollectionOffset, Constants.LargeCollectionOffset);
    }

    private static byte[] GetPrefix(byte[] array, int shortOffset, int largeOffset)
    {
        var length = array.Length;

        if (length == 1)
        {
            if (array[0] == 0) return new[] { (byte)Constants.ShortItemOffset };

            if (array[0] < Constants.ShortItemOffset) return array;
        }

        if (length < Constants.SizeThreshold)
        {
            var shortPrefix = new[] { (byte)(shortOffset + length) };
            return Helpers.JoinByteArrays(shortPrefix, array);
        }

        var lengthBytes = EncodeLength(length);
        var lengthValue = (byte)(largeOffset + lengthBytes.Length);
        var prefix = Helpers.JoinByteArrays(new[] { lengthValue }, lengthBytes);

        return Helpers.JoinByteArrays(prefix, array);
    }

    private static byte[] EncodeLength(int length)
    {
        if (length < 0)
            throw new ArgumentOutOfRangeException(nameof(length));

        var bytes = new List<byte>();
        do
        {
            bytes.Add((byte)(length & 0xff));
            length >>= 8;
        } while (length > 0);

        bytes.Reverse();
        return bytes.ToArray();
    }
}