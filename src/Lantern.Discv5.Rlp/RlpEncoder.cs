using System.Buffers.Binary;
using System.Text;

namespace Lantern.Discv5.Rlp;

public static class RlpEncoder
{
    public static byte[] EncodeInteger(int value)
    {
        return Encode(ToBigEndianBytes(value), false);
    }
    
    public static byte[] EncodeUlong(ulong value)
    {
        return Encode(ToBigEndianBytes(value), false);
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

    public static byte[] EncodeBytes(IEnumerable<byte> item) => Encode(item.ToArray(), false);
    
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
    
    public static byte[] EncodeCollectionOfBytes(IEnumerable<byte> item) => Encode(item.ToArray(), true);
    
    public static byte[] EncodeCollectionsOfBytes(params byte[][] items)
    {
        using var stream = new MemoryStream();

        foreach (var item in items)
        {
            stream.Write(EncodeCollectionOfBytes(item).ToArray());
        }
        
        return Encode(stream.ToArray(), true);
    }

    private static byte[] Encode(byte[] array, bool isCollection)
    {
        if (!isCollection)
        {
            return GetPrefix(array, Constant.ShortItemOffset, Constant.LargeItemOffset);
        }
        
        return GetPrefix(array, Constant.ShortCollectionOffset, Constant.LargeCollectionOffset);
    }

    private static byte[] GetPrefix(byte[] array, int shortOffset, int largeOffset)
    {
        var length = array.Length;

        if (length == 1)
        {
            if (array[0] == 0)
            {
                return new [] { (byte)Constant.ShortItemOffset };
            }

            if(array[0] < Constant.ShortItemOffset)
            {
                return array;
            }
        }
        
        if (length < Constant.SizeThreshold)
        {
            var shortPrefix = new[] { (byte)(shortOffset + length) };
            return Concat(shortPrefix, array);
        }
        
        var lengthBytes = EncodeLength(length);
        var lengthValue = (byte)(largeOffset + lengthBytes.Length);
        var prefix = Concat( new[] { lengthValue }, lengthBytes);
        
        return Concat(prefix, array);
    }

    private static byte[] ToBigEndianBytes(int val)
    {
        var bytes = new byte[4];
        BinaryPrimitives.WriteInt32BigEndian(bytes, val);
        return bytes.AsSpan().TrimStart((byte)0).ToArray();
    }

    private static byte[] ToBigEndianBytes(ulong val)
    {
        var bytes = new byte[8];
        BinaryPrimitives.WriteUInt64BigEndian(bytes, val);
        return bytes.AsSpan().TrimStart((byte)0).ToArray();
    }

    private static byte[] Concat(byte[] a, byte[] b)
    {
        using var ms = new MemoryStream();
        ms.Write(a, 0, a.Length);
        ms.Write(b, 0, b.Length);
        return ms.ToArray();
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
        } 
        while (length > 0);
        bytes.Reverse();
        return bytes.ToArray();
    }
}