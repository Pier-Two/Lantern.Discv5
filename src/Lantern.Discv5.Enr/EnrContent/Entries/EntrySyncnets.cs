using System.Text;
using Lantern.Discv5.Enr.EnrContent.Interfaces;
using Lantern.Discv5.Rlp;

namespace Lantern.Discv5.Enr.EnrContent.Entries;

public class EntrySyncnets : IContentEntry
{
    public EntrySyncnets(byte[] value)
    {
        Value = value;
    }

    public byte[] Value { get; }

    public EnrContentKey Key => EnrContentKey.Syncnets;

    public IEnumerable<byte> EncodeEntry()
    {
        return ByteArrayUtils.JoinByteArrays(RlpEncoder.EncodeString(Key, Encoding.ASCII),
            RlpEncoder.EncodeBytes(Value));
    }
}