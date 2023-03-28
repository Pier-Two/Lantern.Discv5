using System.Text;
using Lantern.Discv5.Rlp;

namespace Lantern.Discv5.Enr.Content.EntryTypes;

public class EntrySecp256K1 : IContentEntry
{
    public EntrySecp256K1(byte[] value)
    {
        Value = value;
    }

    public byte[] Value { get; }

    public string Key => EnrContentKey.Secp256K1;

    public IEnumerable<byte> EncodeEntry()
    {
        return ByteArrayUtils.JoinByteArrays(RlpEncoder.EncodeString(Key, Encoding.ASCII),
            RlpEncoder.EncodeBytes(Value));
    }
}