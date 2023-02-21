using System.Text;
using Lantern.Discv5.Rlp;

namespace Lantern.Discv5.Enr;

public static class RlpExtensions
{
    public static ReadOnlySpan<byte> Encode(string value)
    {
        return RlpEncoder.EncodeItem(Encoding.ASCII.GetBytes(value));
    }

    public static ReadOnlySpan<byte> Encode(int integer)
    {
        return RlpEncoder.EncodeItem(ToBytes(integer));
    }

    public static ReadOnlySpan<byte> Encode(ulong integer)
    {
        return RlpEncoder.EncodeItem(ToBytes(integer));
    }

    public static ReadOnlySpan<byte> Encode(long integer)
    {
        return RlpEncoder.EncodeItem(ToBytes(integer));
    }

    public static ReadOnlySpan<byte> Encode(byte[] item)
    {
        return RlpEncoder.EncodeItem(item);
    }

    public static ReadOnlySpan<byte> EncodeList(params byte[] item)
    {
        return RlpEncoder.EncodeCollection(item);
    }

    public static byte[] ToBytes(int value)
    {
        return LittleToBigEndianConverter(BitConverter.GetBytes(value));
    }

    public static byte[] ToBytes(ulong value)
    {
        return LittleToBigEndianConverter(BitConverter.GetBytes(value));
    }

    public static byte[] ToBytes(long value)
    {
        return LittleToBigEndianConverter(BitConverter.GetBytes(value));
    }

    private static byte[] LittleToBigEndianConverter(byte[] value)
    {
        return value.ReverseIf(BitConverter.IsLittleEndian)
            .SkipWhile(b => b == 0x00)
            .ToArray();
    }

    private static IEnumerable<T> ReverseIf<T>(this IEnumerable<T> source, bool condition)
    {
        return condition ? source.Reverse() : source;
    }
}