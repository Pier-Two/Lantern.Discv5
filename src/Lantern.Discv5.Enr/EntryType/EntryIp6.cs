using System.Net;
using System.Text;
using Lantern.Discv5.Rlp;

namespace Lantern.Discv5.Enr.EntryType;

public class EntryIp6 : IEnrContentEntry
{
    public EntryIp6(IPAddress value)
    {
        Value = value;
    }

    public IPAddress Value { get; }

    public string Key => EnrContentKey.Ip6;

    public byte[] EncodeEntry()
    {
        return Helpers.JoinByteArrays(RlpEncoder.EncodeString(Key, Encoding.ASCII),
            RlpEncoder.EncodeBytes(Value.GetAddressBytes()));
    }
}