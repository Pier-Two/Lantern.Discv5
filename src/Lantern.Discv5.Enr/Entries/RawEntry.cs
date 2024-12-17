using System.Text;
using Lantern.Discv5.Rlp;

namespace Lantern.Discv5.Enr.Entries;

public class RawEntry(string key, byte[] value) : IEntry
{
    public string Key { get; } = key;
    public byte[] Value { get; } = value;

    EnrEntryKey IEntry.Key => new(Key);

    public IEnumerable<byte> EncodeEntry()
    {
        return ByteArrayUtils.JoinByteArrays(RlpEncoder.EncodeString(Key, Encoding.ASCII),
            RlpEncoder.EncodeBytes(Value));
    }
}