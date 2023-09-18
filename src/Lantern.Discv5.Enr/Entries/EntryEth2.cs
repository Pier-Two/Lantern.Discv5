using System.Text;
using Lantern.Discv5.Rlp;

namespace Lantern.Discv5.Enr.Entries;

public class EntryEth2 : IEntry
{
    public EntryEth2(byte[] value)
    {
        Value = value;
    }

    public byte[] Value { get; }

    public EnrEntryKey Key => EnrEntryKey.Eth2;

    public IEnumerable<byte> EncodeEntry()
    {
        return ByteArrayUtils.JoinByteArrays(RlpEncoder.EncodeString(Key, Encoding.ASCII),
            RlpEncoder.EncodeBytes(Value));
    }
}