using System.Text;
using Lantern.Discv5.Rlp;

namespace Lantern.Discv5.Enr.EntryType;

public class EntryUdp6 : IEnrContentEntry
{
    public EntryUdp6(int value)
    {
        Value = value;
    }

    public int Value { get; }

    public string Key => EnrContentKey.Udp6;

    public byte[] EncodeEntry()
    {
        return Helpers.JoinByteArrays(RlpEncoder.EncodeString(Key, Encoding.ASCII),
            RlpEncoder.EncodeInteger(Value));
    }
}