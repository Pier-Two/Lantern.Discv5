using Lantern.Discv5.Rlp;

namespace Lantern.Discv5.Enr.EntryType;

public class EntrySecp256K1 : EnrContentEntry<byte[]>
{
    public EntrySecp256K1(byte[] value) : base(value)
    {
    }

    public override string Key => EnrContentKey.Secp256K1;

    public override byte[] EncodeEntry()
    {
        var keyBytes = RlpExtensions.Encode(Key);
        var valueBytes = RlpExtensions.Encode(Value);
        return Helpers.ConcatenateByteArrays(keyBytes, valueBytes).ToArray();
    }
}