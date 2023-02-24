namespace Lantern.Discv5.Rlp;

public struct Constant
{
    public const int SizeThreshold = 55;
    public const int ShortItemOffset = 128;
    public const int LargeItemOffset = 183;
    public const int ShortCollectionOffset = 192;
    public const int LargeCollectionOffset = 247;
    public const int MaxItemLength = 255;
    // Test setup for pipeline. Will be deprecated
    public const int Test = 2;
}