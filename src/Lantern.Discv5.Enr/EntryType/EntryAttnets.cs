using System.Text;
using Lantern.Discv5.Rlp;

namespace Lantern.Discv5.Enr.EntryType;

public class EntryAttnets : IContentEntry
{
    public EntryAttnets(byte[] value)
    {
        Value = value;
    }

    public byte[] Value { get; }

    public string Key => EnrContentKey.Attnets;

    public byte[] EncodeEntry()
    {
        return ByteArrayUtils.JoinByteArrays(RlpEncoder.EncodeString(Key, Encoding.ASCII),
            RlpEncoder.EncodeBytes(Value));
    }
}