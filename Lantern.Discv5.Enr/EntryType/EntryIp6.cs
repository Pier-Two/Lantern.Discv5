using System.Net;
using Lantern.Discv5.Rlp;

namespace Lantern.Discv5.Enr.EntryType;

public class EntryIp6 : EnrContentEntry<IPAddress>
{
    public EntryIp6(IPAddress address) : base(address)
    {
    }

    public override string Key => EnrContentKey.Ip6;

    public override byte[] EncodeEntry()
    {
        var keyBytes = RlpExtensions.Encode(Key);
        var valueBytes = RlpExtensions.Encode(Value.GetAddressBytes());
        return Helpers.ConcatenateByteArrays(keyBytes, valueBytes).ToArray();
    }
}