namespace Lantern.Discv5.Rlp;

/// <summary>
/// RlpDecoder class handles the RLP (Recursive Length Prefix) decoding of byte arrays.
/// </summary>
public static class RlpDecoder
{
    public readonly struct RlpStruct(bool isString, byte[] src, int index, int prefixLength, int length)
    {
        public bool IsString { get; } = isString;
        public byte[] Source { get; } = src;
        public int Start { get; } = index;
        public int PrefixLength { get; } = prefixLength;
        public int Length { get; } = length;

        public byte[] GetData() => Source[(Start + PrefixLength)..(Start + PrefixLength + Length)];
        public byte[] GetRlp() => Source[Start..(Start + PrefixLength + Length)];

        public Memory<byte> InnerMemory => Source.AsMemory(Start + PrefixLength, Length);
        public ReadOnlySpan<byte> InnerSpan => Source.AsSpan(Start + PrefixLength, Length);
    }

    /// <summary>
    /// Decodes RLP
    /// </summary>
    /// <param name="input">RLP encoded data</param>
    /// <param name="unwrapList">Whether data is an RLP-prefixed list</param>
    /// <returns>Items inside the data</returns>
    public static ReadOnlySpan<RlpStruct> Decode(ReadOnlySpan<byte> input)
    {
        var list = new List<RlpStruct>();
        var index = 0;

        while (index < input.Length)
        {
            var currentByte = input[index];
            int length, lengthOfLength;

            //if (currentByte <= Constants.ShortItemOffset - 1)
            //{
            //    list.Add(new Rlp(true, input, index, 0, 1));
            //    index++;
            //}
            //else if (currentByte <= Constants.LargeItemOffset)
            //{
            //    length = currentByte - Constants.ShortItemOffset;
            //    list.Add(new Rlp(input, index + 1, length));
            //    index += 1 + length;
            //}
            //else if (currentByte <= Constants.ShortCollectionOffset - 1)
            //{
            //    lengthOfLength = currentByte - Constants.LargeItemOffset;
            //    length = RlpExtensions.ByteArrayToInt32(GetBytes(input, index + 1, lengthOfLength).ToArray());
            //    list.Add(GetBytes(input, index + 1 + lengthOfLength, length));
            //    index += 1 + lengthOfLength + length;
            //}
            //else if (currentByte <= Constants.LargeCollectionOffset)
            //{
            //    length = currentByte - Constants.ShortCollectionOffset;
            //    list.Add(GetBytes(input, index + 1, length));
            //    index += 1 + length;
            //}
            //else
            //{
            //    lengthOfLength = currentByte - Constants.LargeCollectionOffset;
            //    length = RlpExtensions.ByteArrayToInt32(GetBytes(input, index + 1, lengthOfLength).ToArray());
            //    list.Add(GetBytes(input, index + 1 + lengthOfLength, length));
            //    index += 1 + lengthOfLength + length;
            //}
        }

        return list.ToArray();
    }

    /// <summary>
    /// Get the next bytes from the byte array.
    /// </summary>
    /// <param name="byteArray">The input byte array.</param>
    /// <param name="index">The starting index to get the bytes from.</param>
    /// <param name="count">The number of bytes to retrieve.</param>
    /// <returns>A byte array containing the requested bytes.</returns>
    private static Memory<byte> GetBytes(Memory<byte> byteArray, int index, int count)
    {
        if (index < 0 || index >= byteArray.Length)
            throw new ArgumentException("The provided index is out of range.");

        if (count < 0) throw new ArgumentException("The provided count parameter must be a non-negative integer.");

        if (index + count > byteArray.Length)
            throw new ArgumentException("The requested range is out of bounds of the byte array.");

        return byteArray.Slice(index, count);
    }
}
