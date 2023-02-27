namespace Lantern.Discv5.Rlp;

public static class RlpDecoder
{
    public static IList<byte[]> Decode(ReadOnlySpan<byte> input)
    {
        var message = new RlpMessage(input);

        while (message.Remainder.Offset < input.Length) Decode(message);

        return message.Data;
    }

    private static void Decode(RlpMessage msg)
    {
        var firstByte = Convert.ToInt16(msg.Remainder.Array![msg.Remainder.Offset]);

        switch (firstByte)
        {
            case <= Constant.ShortItemOffset - 1:
                DecodeSingleByte(msg);
                break;
            case <= Constant.LargeItemOffset:
                DecodeString(msg);
                break;
            case <= Constant.ShortCollectionOffset - 1:
                DecodeShortCollection(msg);
                break;
            case <= Constant.LargeCollectionOffset:
                DecodeCollection(msg);
                break;
            case > Constant.MaxItemLength:
                throw new ArgumentOutOfRangeException(nameof(msg), "msg is too long");
            default:
                DecodeLongCollection(msg);
                break;
        }
    }

    private static void DecodeSingleByte(RlpMessage msg)
    {
        msg.Data.Add(new[] { msg.Remainder.Array![msg.Remainder.Offset] });
        msg.Remainder = msg.Remainder[1..];
    }

    private static void DecodeString(RlpMessage msg)
    {
        var itemLength =
            Math.Abs(Constant.ShortItemOffset - Convert.ToInt16(msg.Remainder.Array![msg.Remainder.Offset]));
        var data = GetArraySegment(msg.Remainder, 1, itemLength);
        msg.Data.Add(data.ToArray());
        msg.Remainder = msg.Remainder[(data.Count + 1)..];
    }

    private static void DecodeShortCollection(RlpMessage msg)
    {
        var listLength =
            Math.Abs(Constant.LargeItemOffset - Convert.ToInt16(msg.Remainder.Array![msg.Remainder.Offset]));
        var itemLength = Convert.ToInt16(msg.Remainder.Array[msg.Remainder.Offset + 1]);
        var data = GetArraySegment(msg.Remainder, listLength + 1, itemLength);
        msg.Data.Add(data.ToArray());
        msg.Remainder = msg.Remainder[(data.Offset + data.Count)..];
    }

    private static void DecodeCollection(RlpMessage msg)
    {
        var itemLength = Math.Abs(Constant.ShortCollectionOffset -
                                  Convert.ToInt16(msg.Remainder.Array![msg.Remainder.Offset]));
        var data = GetArraySegment(msg.Remainder, 1, itemLength).ToArray();

        while (msg.Remainder.Offset < msg.Remainder.Array!.Length)
        {
            var decoded = Decode(data);
            msg.Data.AddRange(decoded);
            msg.Remainder = msg.Remainder[msg.Remainder.Count..];
        }
    }

    private static void DecodeLongCollection(RlpMessage msg)
    {
        var listLength = Math.Abs(Constant.LargeCollectionOffset -
                                  Convert.ToInt16(msg.Remainder.Array![msg.Remainder.Offset]));
        var itemLength = Convert.ToInt16(msg.Remainder.Array[msg.Remainder.Offset + 1]);
        var data = GetArraySegment(msg.Remainder, listLength + 1, itemLength).ToArray();

        while (msg.Remainder.Offset < msg.Remainder.Array!.Length)
        {
            var decoded = Decode(data);
            msg.Data.AddRange(decoded);
            msg.Remainder = msg.Remainder[msg.Remainder.Count..];
        }
    }

    private static ArraySegment<byte> GetArraySegment(ArraySegment<byte> array, int offset, int count)
    {
        return new ArraySegment<byte>(array.Array!, array.Offset + offset, count);
    }
}