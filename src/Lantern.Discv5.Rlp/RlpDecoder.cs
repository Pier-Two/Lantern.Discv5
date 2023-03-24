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

    private static byte[] DecodeSingleByte(byte[] encodedData, int index)
    {
        return new[] { encodedData[index] };
    }

    private static byte[] DecodeString(byte[] encodedData, int index, int length)
    {
        return encodedData.SubArray(index, length);
    }

    private static List<byte[]> DecodeList(byte[] encodedData, int index, int length)
    {
        return Decode(encodedData.SubArray(index, length));
    }

    private static byte[] GetNextBytes(byte[] byteArray, int index, int count)
    {
        if (index < 0 || index >= byteArray.Length)
            throw new ArgumentException("The provided index is out of range.");

        if (count < 0) throw new ArgumentException("The provided count parameter must be a non-negative integer.");

        if (index + count > byteArray.Length)
            throw new ArgumentException("The requested range is out of bounds of the byte array.");

        return byteArray.SubArray(index, count);
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
