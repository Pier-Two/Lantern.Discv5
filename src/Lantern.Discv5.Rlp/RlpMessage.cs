namespace Lantern.Discv5.Rlp;

internal class RlpMessage
{
    public RlpMessage(ReadOnlySpan<byte> input)
    {
        Data = new List<byte[]>();
        Remainder = new ArraySegment<byte>(input.ToArray());
    }

    public List<byte[]> Data { get; }

    public ArraySegment<byte> Remainder { get; set; }
}