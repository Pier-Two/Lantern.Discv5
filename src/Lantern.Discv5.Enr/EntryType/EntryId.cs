using System.Text;
using Lantern.Discv5.Rlp;

namespace Lantern.Discv5.Enr.EntryType;

public class EntryId : IEnrContentEntry
{
    public EntryId(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public string Key => EnrContentKey.Id;

    public byte[] EncodeEntry()
    {
        return Helpers.JoinByteArrays(RlpEncoder.EncodeString(Key, Encoding.ASCII),
            RlpEncoder.EncodeString(Value, Encoding.ASCII));
    }
}