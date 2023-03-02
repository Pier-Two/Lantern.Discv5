namespace Lantern.Discv5.Rlp;

public static class RlpDecoder
{
    public static List<byte[]> Decode(byte[] encodedData, int maxItems = int.MaxValue)
    {
        var result = new List<object>();

        var index = 0;
        while (index < encodedData.Length && result.Count < maxItems)
        {
            var currentByte = encodedData[index];
            switch (currentByte)
            {
                case < 0x80:
                    result.Add(DecodeSingleByte(encodedData, index));
                    index++;
                    break;
                case < 0xb8:
                {
                    var length = currentByte - 0x80;
                    result.Add(DecodeString(encodedData, index, length));
                    index += 1 + length;
                    break;
                }
                case < 0xc0:
                {
                    var lengthBytes = currentByte - 0xb7;
                    var length = DecodeLength(encodedData, index + 1, lengthBytes);
                    result.Add(DecodeString(encodedData, index + lengthBytes + 1, length));
                    index += lengthBytes + length + 1;
                    break;
                }
                case < 0xf8:
                {
                    var length = currentByte - 0xc0;
                    result.Add(DecodeList(encodedData, index + 1, length));
                    index += 1 + length;
                    break;
                }
                default:
                {
                    var lengthBytes = currentByte - 0xf7;
                    var length = DecodeLength(encodedData, index + 1, lengthBytes);
                    result.Add(DecodeList(encodedData, index + lengthBytes + 1, length));
                    index += lengthBytes + length + 1;
                    break;
                }
            }
        }

        var finalResult = Flatten(result)
            .Take(maxItems)
            .ToList();
        
        return finalResult;
    }

    private static byte[] DecodeSingleByte(byte[] encodedData, int index)
    {
        return new [] { encodedData[index] };
    }

    private static byte[] DecodeString(byte[] encodedData, int index, int length)
    {
        return encodedData.SubArray(index + 1, length);
    }

    private static List<byte[]> DecodeList(byte[] encodedData, int index, int length)
    {
        var sublistData = encodedData.SubArray(index, length);
        return Decode(sublistData);
    }

    private static int DecodeLength(byte[] encodedData, int index, int lengthBytes)
    {
        var length = 0;
        for (var i = 0; i < lengthBytes; i++)
        {
            length = (length << 8) + encodedData[index + i];
        }
        return length;
    }

    private static List<byte[]> Flatten(List<object> list)
    {
        var result = new List<byte[]>();
        foreach (var item in list)
        {
            switch (item)
            {
                case byte[] singleByte:
                    result.Add(singleByte);
                    break;
                case List<byte[]> sublist:
                    result.AddRange(sublist);
                    break;
            }
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

