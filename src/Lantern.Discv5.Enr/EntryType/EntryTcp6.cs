using System.Text;
using Lantern.Discv5.Rlp;

namespace Lantern.Discv5.Enr.EntryType;

public class EntryTcp6 : IEnrContentEntry
{
    public EntryTcp6(int value)
    {
        Value = value;
    }

    public int Value { get; }

    public string Key => EnrContentKey.Tcp6;

    public byte[] EncodeEntry()
    {
        return Helpers.JoinByteArrays(RlpEncoder.EncodeString(Key, Encoding.ASCII),
            RlpEncoder.EncodeInteger(Value));
    }
}