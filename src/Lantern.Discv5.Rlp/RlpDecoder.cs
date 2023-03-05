namespace Lantern.Discv5.Rlp;

public static class RlpDecoder
{
    public static List<byte[]> Decode(byte[] input)
    {
        var list = new List<object>();
        var index = 0;

        while (index < input.Length)
        {
            var currentByte = input[index];
            switch (currentByte)
            {
                // Single byte
                case >= Constants.ZeroByte and <= Constants.ShortItemOffset - 1:
                    list.Add(DecodeSingleByte(input, index));
                    index++;
                    break;
                // Short item
                case >= Constants.ShortItemOffset and <= Constants.LargeItemOffset:
                {
                    var length = currentByte - Constants.ShortItemOffset;
                    list.Add(DecodeString(input, index, length));
                    index += 1 + length;
                    break;
                }
                // Long item
                case >= Constants.LargeItemOffset + 1 and <= Constants.ShortCollectionOffset - 1:
                {
                    var lengthOfLength = currentByte - Constants.LargeItemOffset;
                    var lengthBytes = GetNextBytes(input, index + 1, lengthOfLength);
                    var length = RlpExtensions.ByteArrayToInt32(lengthBytes);
                    list.Add(DecodeString(input, index + lengthOfLength, length));
                    index += lengthOfLength + length + 1;
                    break;
                }
                // Short collection
                case >= Constants.ShortCollectionOffset and <= Constants.LargeCollectionOffset:
                {
                    var length = currentByte - Constants.ShortCollectionOffset;
                    list.Add(DecodeList(input, index, length));
                    index += 1 + length;
                    break;
                }
                // Long collection
                case >= Constants.LargeCollectionOffset + 1 and <= Constants.MaxItemLength:
                {
                    var lengthOfLength = currentByte - Constants.LargeCollectionOffset;
                    var lengthBytes = GetNextBytes(input, index + 1, lengthOfLength);
                    var length = RlpExtensions.ByteArrayToInt32(lengthBytes);
                    var sublist = DecodeList(input, index + lengthOfLength, length);
                    list.AddRange(sublist);
                    index += lengthOfLength + length + 1;
                    break;
                }
            }
        }

        return list.Flatten();
    }

    private static byte[] DecodeSingleByte(byte[] encodedData, int index)
    {
        return new[] { encodedData[index] };
    }

    private static byte[] DecodeString(byte[] encodedData, int index, int length)
    {
        return encodedData.SubArray(index + 1, length);
    }

    private static List<byte[]> DecodeList(byte[] encodedData, int index, int length)
    {
        var subArray = encodedData.SubArray(index + 1, length);
        return Decode(subArray);
    }

    public static byte[] GetNextBytes(byte[] byteArray, int index, int count)
    {
        if (index < 0 || index >= byteArray.Length)
            throw new IndexOutOfRangeException("The provided index is out of range.");

        if (count < 0) throw new ArgumentException("The provided count parameter must be a non-negative integer.");

        if (index + count > byteArray.Length)
            throw new ArgumentException("The requested range is out of bounds of the byte array.");

        var resultArray = new byte[count];
        Array.Copy(byteArray, index, resultArray, 0, count);

        return resultArray;
    }

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

    private static T[] SubArray<T>(this T[] array, int index, int length)
    {
        var result = new T[length];
        Array.Copy(array, index, result, 0, length);
        return result;
    }
}