namespace Lantern.Discv5.Rlp;

/// <summary>
/// RlpDecoder class handles the RLP (Recursive Length Prefix) decoding of byte arrays.
/// </summary>
public static class RlpDecoder
{
    /// <summary>
    /// Decode RLP encoded input into a list of byte arrays.
    /// </summary>
    /// <param name="input">The RLP encoded input byte array.</param>
    /// <returns>A list of byte arrays containing the decoded data.</returns>
    public static List<byte[]> Decode(byte[] input)
    {
        var list = new List<object>();
        var index = 0;

        while (index < input.Length)
        {
            var currentByte = input[index];
            int length, lengthOfLength;

            if (currentByte <= Constants.ShortItemOffset - 1)
            {
                list.Add(DecodeSingleByte(input, index));
                index++;
            }
            else if (currentByte <= Constants.LargeItemOffset)
            {
                length = currentByte - Constants.ShortItemOffset;
                list.Add(DecodeString(input, index + 1, length));
                index += 1 + length;
            }
            else if (currentByte <= Constants.ShortCollectionOffset - 1)
            {
                lengthOfLength = currentByte - Constants.LargeItemOffset;
                length = RlpExtensions.ByteArrayToInt32(GetNextBytes(input, index + 1, lengthOfLength));
                list.Add(DecodeString(input, index + 1 + lengthOfLength, length));
                index += 1 + lengthOfLength + length;
            }
            else if (currentByte <= Constants.LargeCollectionOffset)
            {
                length = currentByte - Constants.ShortCollectionOffset;
                list.Add(DecodeList(input, index + 1, length));
                index += 1 + length;
            }
            else
            {
                lengthOfLength = currentByte - Constants.LargeCollectionOffset;
                length = RlpExtensions.ByteArrayToInt32(GetNextBytes(input, index + 1, lengthOfLength));
                list.AddRange(DecodeList(input, index + 1 + lengthOfLength, length));
                index += 1 + lengthOfLength + length;
            }
        }

        return list.Flatten();
    }

    /// <summary>
    /// Decode a single byte item from the encoded data.
    /// </summary>
    /// <param name="encodedData">The RLP encoded input byte array.</param>
    /// <param name="index">The index of the item in the encoded data.</param>
    /// <returns>A byte array containing the decoded single byte item.</returns>
    private static byte[] DecodeSingleByte(byte[] encodedData, int index)
    {
        return new[] { encodedData[index] };
    }

    /// <summary>
    /// Decode a string item from the encoded data.
    /// </summary>
    /// <param name="encodedData">The RLP encoded input byte array.</param>
    /// <param name="index">The starting index of the item in the encoded data.</param>
    /// <param name="length">The length of the item to decode.</param>
    /// <returns>A byte array containing the decoded string item.</returns>
    private static byte[] DecodeString(byte[] encodedData, int index, int length)
    {
        return encodedData[index..(index + length)];
    }

    /// <summary>
    /// Decode a list item from the encoded data.
    /// </summary>
    /// <param name="encodedData">The RLP encoded input byte array.</param>
    /// <param name="index">The starting index of the list item in the encoded data.</param>
    /// <param name="length">The length of the list item to decode.</param>
    /// <returns>A list of byte arrays containing the decoded list items.</returns>
    private static List<byte[]> DecodeList(byte[] encodedData, int index, int length)
    {
        return Decode(encodedData[index..(index + length)]);
    }

    /// <summary>
    /// Get the next bytes from the byte array.
    /// </summary>
    /// <param name="byteArray">The input byte array.</param>
    /// <param name="index">The starting index to get the bytes from.</param>
    /// <param name="count">The number of bytes to retrieve.</param>
    /// <returns>A byte array containing the requested bytes.</returns>
    private static byte[] GetNextBytes(byte[] byteArray, int index, int count)
    {
        if (index < 0 || index >= byteArray.Length)
            throw new ArgumentException("The provided index is out of range.");

        if (count < 0) throw new ArgumentException("The provided count parameter must be a non-negative integer.");

        if (index + count > byteArray.Length)
            throw new ArgumentException("The requested range is out of bounds of the byte array.");

        return byteArray[index..(index + count)];
    }

    /// <summary>
    /// Flatten a list containing byte arrays and lists of byte arrays into a single list of byte arrays.
    /// </summary>
    /// <param name="list">The input list containing byte arrays and lists of byte arrays.</param>
    /// <returns>A flattened list containing only byte arrays.</returns>
    private static List<byte[]> Flatten(this List<object> list)
    {
        var result = new List<byte[]>();

        foreach (var item in list)
            switch (item)
            {
                case byte[] singleByte:
                    result.Add(singleByte);
                    break;
                case List<byte[]> sublist:
                    result.AddRange(sublist);
                    break;
            }

        return result;
    }
}
