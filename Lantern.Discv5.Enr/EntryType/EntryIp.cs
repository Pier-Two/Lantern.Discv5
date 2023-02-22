using System.Net;
using Lantern.Discv5.Rlp;

namespace Lantern.Discv5.Enr.EntryType;

public class EntryIp : IEnrContentEntry
{
    public EntryIp(IPAddress value)
    {
        Value = value;
    }

    public IPAddress Value { get; }

    public string Key => EnrContentKey.Ip;

    public byte[] EncodeEntry()
    {
        var keyBytes = RlpExtensions.Encode(Key);
        var valueBytes = RlpExtensions.Encode(Value.GetAddressBytes());
        return Helpers.ConcatenateByteArrays(keyBytes, valueBytes).ToArray();
    }
}