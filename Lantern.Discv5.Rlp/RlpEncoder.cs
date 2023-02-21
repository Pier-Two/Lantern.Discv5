namespace Lantern.Discv5.Rlp;

public static class RlpEncoder
{
    public static ReadOnlySpan<byte> EncodeItem(ReadOnlySpan<byte> item)
    {
        return Encode(item, Constant.ShortItemOffset, Constant.LargeItemOffset);
    }

    public static ReadOnlySpan<byte> EncodeCollection(ReadOnlySpan<byte> item)
    {
        return Encode(item, Constant.ShortItemOffset, Constant.LargeItemOffset);
    }

    public static byte[] GetPayloadPrefix(long length)
    {
        if (length <= Constant.SizeThreshold + 1) return new[] { (byte)(Constant.LargeCollectionOffset + length) };

        if (length <= byte.MaxValue) return new[] { (byte)(Constant.LargeCollectionOffset + 1), (byte)length };

        var lengthBytes = BitConverter.GetBytes(length);
        var prefixArray = new byte[1 + lengthBytes.Length];
        prefixArray[0] = (byte)(Constant.LargeCollectionOffset + lengthBytes.Length);
        lengthBytes.CopyTo(prefixArray.AsSpan(1));
        return prefixArray;
    }

    private static ReadOnlySpan<byte> Encode(ReadOnlySpan<byte> item, int shortOffset, int longOffset)
    {
        var length = item.Length;

        if (length == 1 && item[0] < shortOffset)
            return item;

        if (length <= Constant.SizeThreshold)
        {
            var prefix = new[] { (byte)(shortOffset + length) };
            return Helpers.ConcatenateByteArrays(prefix, item);
        }

        if (length > Constant.MaxItemLength) throw new ArgumentOutOfRangeException(nameof(item), "item is too long");
        {
            var lengthBytes = new[] { (byte)length };
            var prefix = (byte)(longOffset + lengthBytes.Length);
            var prefixArray = new[] { prefix, (byte)length };
            return Helpers.ConcatenateByteArrays(prefixArray, item);
        }
    }
}