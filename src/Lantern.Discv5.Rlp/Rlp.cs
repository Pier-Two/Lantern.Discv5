namespace Lantern.Discv5.Rlp;

public readonly struct Rlp(ReadOnlyMemory<byte> src, int prefixLength)
{
    public ReadOnlyMemory<byte> Source { get; } = src;
    public int PrefixLength { get; } = prefixLength;

    public byte[] GetData() => Source[PrefixLength..].ToArray();
    public byte[] GetRlp() => Source.ToArray();

    public ReadOnlyMemory<byte> InnerSpan => Source[PrefixLength..];
}
