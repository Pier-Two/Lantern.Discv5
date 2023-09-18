using System.Text;
using Lantern.Discv5.Rlp;

namespace Lantern.Discv5.Enr.Entries;

public class EntrySyncnets : IEntry
{
    public EntrySyncnets(byte[] value)
    {
        Value = value;
    }

    public byte[] Value { get; }

    public EnrEntryKey Key => EnrEntryKey.Syncnets;

    public IEnumerable<byte> EncodeEntry()
    {
        return ByteArrayUtils.JoinByteArrays(RlpEncoder.EncodeString(Key, Encoding.ASCII),
            RlpEncoder.EncodeBytes(Value));
    }
}