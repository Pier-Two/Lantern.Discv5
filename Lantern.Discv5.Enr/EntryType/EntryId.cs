using Lantern.Discv5.Rlp;

namespace Lantern.Discv5.Enr.EntryType;

public class EntryId : IEnrContentEntry
{
    public EntryId(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public string Key => EnrContentKey.Id;

    public byte[] EncodeEntry()
    {
        var keyBytes = RlpExtensions.Encode(Key);
        var valueBytes = RlpExtensions.Encode(Value);

        return Helpers.ConcatenateByteArrays(keyBytes, valueBytes).ToArray();
    }
}