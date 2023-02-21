using Lantern.Discv5.Rlp;

namespace Lantern.Discv5.Enr.EntryType;

public class EntryId : EnrContentEntry<string>
{
    public EntryId(string value) : base(value)
    {
    }

    public override string Key => EnrContentKey.Id;

    public override byte[] EncodeEntry()
    {
        var keyBytes = RlpExtensions.Encode(Key);
        var valueBytes = RlpExtensions.Encode(Value);

        return Helpers.ConcatenateByteArrays(keyBytes, valueBytes).ToArray();
    }
}