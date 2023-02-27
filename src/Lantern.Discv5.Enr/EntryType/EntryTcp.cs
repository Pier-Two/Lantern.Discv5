using System.Text;
using Lantern.Discv5.Rlp;

namespace Lantern.Discv5.Enr.EntryType;

public class EntryTcp : IEnrContentEntry
{
    public EntryTcp(int value)
    {
        Value = value;
    }

    public int Value { get; }

    public string Key => EnrContentKey.Tcp;

    public byte[] EncodeEntry()
    {
        return Helpers.JoinByteArrays(RlpEncoder.EncodeString(Key, Encoding.ASCII),
            RlpEncoder.EncodeInteger(Value));
    }
}