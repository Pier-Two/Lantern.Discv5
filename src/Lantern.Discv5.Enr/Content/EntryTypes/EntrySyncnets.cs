using System.Text;
using Lantern.Discv5.Rlp;

namespace Lantern.Discv5.Enr.Content.EntryTypes;

public class EntrySyncnets : IContentEntry
{
    public EntrySyncnets(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public string Key => EnrContentKey.Syncnets;

    public IEnumerable<byte> EncodeEntry()
    {
        return ByteArrayUtils.JoinByteArrays(RlpEncoder.EncodeString(Key, Encoding.ASCII),
            RlpEncoder.EncodeHexString(Value));
    }
}