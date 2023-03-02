using System.Buffers.Binary;

namespace Lantern.Discv5.Rlp;

public static class Helpers
{
    public static byte[] JoinMultipleByteArrays(params ReadOnlyMemory<byte>[] byteArrays)
    {
        using var stream = new MemoryStream();
        foreach (var byteArray in byteArrays)
        {
            stream.Write(byteArray.Span);
        }
        return stream.ToArray();
    }
    
    public static byte[] JoinByteArrays(ReadOnlySpan<byte> firstArray, ReadOnlySpan<byte> secondArray)
    {
        using var stream = new MemoryStream(firstArray.Length + secondArray.Length);
        stream.Write(firstArray);
        stream.Write(secondArray);
        return stream.ToArray();
    }
    
    public static byte[] ToBigEndianBytes(int val)
    {
        var bytes = new byte[4];
        BinaryPrimitives.WriteInt32BigEndian(bytes, val);
        return bytes.AsSpan().TrimStart((byte)0).ToArray();
    }

    public static byte[] ToBigEndianBytes(ulong val)
    {
        var bytes = new byte[8];
        BinaryPrimitives.WriteUInt64BigEndian(bytes, val);
        return bytes.AsSpan().TrimStart((byte)0).ToArray();
    }

    public static byte[] Concat(byte[] a, byte[] b)
    {
        using var ms = new MemoryStream();
        ms.Write(a, 0, a.Length);
        ms.Write(b, 0, b.Length);
        return ms.ToArray();
    }

}