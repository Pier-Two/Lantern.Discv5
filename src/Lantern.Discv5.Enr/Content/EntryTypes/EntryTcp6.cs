using System.Text;
using Lantern.Discv5.Rlp;

namespace Lantern.Discv5.Enr.Content.EntryTypes;

public class EntryTcp6 : IContentEntry
{
    public EntryTcp6(int value)
    {
        Value = value;
    }

    public int Value { get; }

    public EnrContentKey Key => EnrContentKey.Tcp6;

    public IEnumerable<byte> EncodeEntry()
    {
        return ByteArrayUtils.JoinByteArrays(RlpEncoder.EncodeString(Key, Encoding.ASCII),
            RlpEncoder.EncodeInteger(Value));
    }
}