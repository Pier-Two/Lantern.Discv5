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

        // single byte
        if (firstByte <= Constant.ShortItemOffset - 1)
        {
            msg.Data.Add(new[] { msg.Remainder.Array[msg.Remainder.Offset] });
            msg.Remainder = msg.Remainder[1..];
            return;
        }

        // string 0-55 bytes
        if (firstByte <= Constant.LargeItemOffset)
        {
            var itemLength = Math.Abs(Constant.ShortItemOffset - firstByte);
            var data = firstByte == 0x80
                ? new ArraySegment<byte>(Array.Empty<byte>())
                : msg.Remainder.Slice(1, itemLength);

            msg.Data.Add(data.ToArray());
            msg.Remainder = msg.Remainder[(data.Count + 1)..];
            return;
        }

        // string 56-255 bytes
        if (firstByte <= Constant.ShortCollectionOffset - 1)
        {
            var listLength = Math.Abs(Constant.LargeItemOffset - firstByte);
            var itemLength = Convert.ToInt16(msg.Remainder.Array[msg.Remainder.Offset + 1]);
            var data = msg.Remainder.Slice(listLength + 1, itemLength);

            msg.Data.Add(data.ToArray());
            msg.Remainder = msg.Remainder[(data.Offset + data.Count)..];
            return;
        }

        // collection 0-55 bytes
        if (firstByte <= Constant.LargeCollectionOffset)
        {
            var itemLength = Math.Abs(Constant.ShortCollectionOffset - firstByte);
            var data = msg.Remainder.Slice(1, itemLength).ToArray();

            while (msg.Remainder.Offset < msg.Remainder.Array!.Length)
            {
                var decoded = Decode(data);
                msg.Data.AddRange(decoded);
                msg.Remainder = msg.Remainder[msg.Remainder.Count..];
            }

            return;
        }

        // collection 56-255 bytes
        if (firstByte > Constant.MaxItemLength) throw new ArgumentOutOfRangeException(nameof(msg), "msg is too long");
        {
            var listLength = Math.Abs(Constant.LargeCollectionOffset - firstByte);
            var itemLength = Convert.ToInt16(msg.Remainder.Array[msg.Remainder.Offset + 1]);
            var data = msg.Remainder.Slice(listLength + 1, itemLength).ToArray();

            while (msg.Remainder.Offset < msg.Remainder.Array!.Length)
            {
                var decoded = Decode(data);
                msg.Data.AddRange(decoded);
                msg.Remainder = msg.Remainder[msg.Remainder.Count..];
            }
        }
    }
}