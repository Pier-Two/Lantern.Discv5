using System.Text;
using Lantern.Discv5.Enr.EnrContent.Interfaces;
using Lantern.Discv5.Rlp;

namespace Lantern.Discv5.Enr.EnrContent.Entries;

public class EntryUdp : IContentEntry
{
    public EntryUdp(int value)
    {
        Value = value;
    }

    public int Value { get; }

    public EnrContentKey Key => EnrContentKey.Udp;

    public IEnumerable<byte> EncodeEntry()
    {
        return ByteArrayUtils.JoinByteArrays(RlpEncoder.EncodeString(Key, Encoding.ASCII),
            RlpEncoder.EncodeInteger(Value));
    }
}