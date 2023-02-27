using System.Text;
using Lantern.Discv5.Rlp;

namespace Lantern.Discv5.Enr.EntryType;

public class EntrySecp256K1 : IEnrContentEntry
{
    public EntrySecp256K1(byte[] value)
    {
        Value = value;
    }

    public byte[] Value { get; }

    public string Key => EnrContentKey.Secp256K1;

    public byte[] EncodeEntry()
    {
        return Helpers.JoinByteArrays(RlpEncoder.EncodeString(Key, Encoding.ASCII),
            RlpEncoder.EncodeBytes(Value));
    }
}