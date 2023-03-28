using System.Net;
using System.Text;
using Lantern.Discv5.Rlp;

namespace Lantern.Discv5.Enr.Content.EntryTypes;

public class EntryIp6 : IContentEntry
{
    public EntryIp6(IPAddress value)
    {
        Value = value;
    }

    public IPAddress Value { get; }

    public string Key => EnrContentKey.Ip6;

    public IEnumerable<byte> EncodeEntry()
    {
        return ByteArrayUtils.JoinByteArrays(RlpEncoder.EncodeString(Key, Encoding.ASCII),
            RlpEncoder.EncodeBytes(Value.GetAddressBytes()));
    }
}