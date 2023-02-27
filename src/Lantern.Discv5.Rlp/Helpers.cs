namespace Lantern.Discv5.Rlp;

public static class Helpers
{
    public static byte[] JoinMultipleByteArrays(params ReadOnlyMemory<byte>[] byteArrays)
    {
        using var stream = new MemoryStream();
        foreach (var byteArray in byteArrays)
        {
            stream.Write(byteArray.Span);
        }
        return stream.ToArray();
    }
    
    public static byte[] JoinByteArrays(ReadOnlySpan<byte> firstArray, ReadOnlySpan<byte> secondArray)
    {
        using var stream = new MemoryStream(firstArray.Length + secondArray.Length);
        stream.Write(firstArray);
        stream.Write(secondArray);
        return stream.ToArray();
    }
}