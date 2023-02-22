using Lantern.Discv5.Rlp;

namespace Lantern.Discv5.Enr.EntryType;

public class EntryUdp : IEnrContentEntry
{
    public EntryUdp(int value)
    {
        Value = value;
    }

    public int Value { get; }

    public string Key => EnrContentKey.Udp;

    public byte[] EncodeEntry()
    {
        var keyBytes = RlpExtensions.Encode(Key);
        var valueBytes = RlpExtensions.Encode(Value);
        return Helpers.ConcatenateByteArrays(keyBytes, valueBytes).ToArray();
    }
}