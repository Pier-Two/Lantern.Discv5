using System.Text;
using Lantern.Discv5.Rlp;

namespace Lantern.Discv5.Enr.EnrContent.Entries;

public class EntryId : IContentEntry
{
    public EntryId(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public EnrContentKey Key => EnrContentKey.Id;

    public IEnumerable<byte> EncodeEntry()
    {
        return ByteArrayUtils.JoinByteArrays(RlpEncoder.EncodeString(Key, Encoding.ASCII),
            RlpEncoder.EncodeString(Value, Encoding.ASCII));
    }
}