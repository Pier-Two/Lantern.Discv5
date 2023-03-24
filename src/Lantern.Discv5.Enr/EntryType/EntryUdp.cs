using System.Text;
using Lantern.Discv5.Rlp;

namespace Lantern.Discv5.Enr.EntryType;

public class EntryUdp : IContentEntry
{
    public EntryUdp(int value)
    {
        Value = value;
    }

    public int Value { get; }

    public string Key => EnrContentKey.Udp;

    public byte[] EncodeEntry()
    {
        return ByteArrayUtils.JoinByteArrays(RlpEncoder.EncodeString(Key, Encoding.ASCII),
            RlpEncoder.EncodeInteger(Value));
    }
}