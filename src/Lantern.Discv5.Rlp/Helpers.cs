using System.Buffers.Binary;

namespace Lantern.Discv5.Rlp;

public static class Helpers
{
    public static byte[] JoinMultipleByteArrays(params ReadOnlyMemory<byte>[] byteArrays)
    {
        using var stream = new MemoryStream();
        foreach (var byteArray in byteArrays) stream.Write(byteArray.Span);
        return stream.ToArray();
    }

    public static byte[] JoinByteArrays(ReadOnlySpan<byte> firstArray, ReadOnlySpan<byte> secondArray)
    {
        var result = new byte[firstArray.Length + secondArray.Length];
        Buffer.BlockCopy(firstArray.ToArray(), 0, result, 0, firstArray.Length);
        Buffer.BlockCopy(secondArray.ToArray(), 0, result, firstArray.Length, secondArray.Length);
        return result;
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
}