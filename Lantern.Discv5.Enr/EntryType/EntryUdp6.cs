using Lantern.Discv5.Rlp;

namespace Lantern.Discv5.Enr.EntryType;

public class EntryUdp6 : EnrContentEntry<int>
{
    public EntryUdp6(int value) : base(value)
    {
    }

    public override string Key => EnrContentKey.Udp6;

    public override byte[] EncodeEntry()
    {
        var keyBytes = RlpExtensions.Encode(Key);
        var valueBytes = RlpExtensions.Encode(Value);
        return Helpers.ConcatenateByteArrays(keyBytes, valueBytes).ToArray();
    }
}