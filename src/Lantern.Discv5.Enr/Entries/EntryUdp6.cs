using System.Text;
using Lantern.Discv5.Rlp;

namespace Lantern.Discv5.Enr.Entries;

public class EntryUdp6 : IEntry
{
    public EntryUdp6(int value)
    {
        Value = value;
    }

    public int Value { get; }

    public EnrEntryKey Key => EnrEntryKey.Udp6;

    public IEnumerable<byte> EncodeEntry()
    {
        return ByteArrayUtils.JoinByteArrays(RlpEncoder.EncodeString(Key, Encoding.ASCII),
            RlpEncoder.EncodeInteger(Value));
    }
}