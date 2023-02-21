namespace Lantern.Discv5.Rlp;

public static class Helpers
{
    public static ReadOnlySpan<byte> ConcatenateByteArrays(ReadOnlySpan<byte> firstArray,
        ReadOnlySpan<byte> secondArray)
    {
        using var stream = new MemoryStream(firstArray.Length + secondArray.Length);
        stream.Write(firstArray);
        stream.Write(secondArray);
        return stream.ToArray();
    }
}