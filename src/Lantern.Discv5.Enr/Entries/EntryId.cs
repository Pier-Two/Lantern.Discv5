using System.Text;
using Lantern.Discv5.Rlp;

namespace Lantern.Discv5.Enr.Entries;

public class EntryId : IEntry
{
    public EntryId(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public EnrEntryKey Key => EnrEntryKey.Id;

    public IEnumerable<byte> EncodeEntry()
    {
        return ByteArrayUtils.JoinByteArrays(RlpEncoder.EncodeString(Key, Encoding.ASCII),
            RlpEncoder.EncodeString(Value, Encoding.ASCII));
    }
}