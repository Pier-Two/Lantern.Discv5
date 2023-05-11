using System.Collections;

namespace Lantern.Discv5.WireProtocol.Utility;

public static class ByteArrayEqualityComparer
{
    public static readonly EqualityComparer<byte[]> Instance = new ByteArrayEqualityComparerImplementation();

    private class ByteArrayEqualityComparerImplementation : EqualityComparer<byte[]>
    {
        public override bool Equals(byte[]? x, byte[]? y)
        {
            return x.SequenceEqual(y);
        }

        public override int GetHashCode(byte[] obj)
        {
            return StructuralComparisons.StructuralEqualityComparer.GetHashCode(obj);
        }
    }
}