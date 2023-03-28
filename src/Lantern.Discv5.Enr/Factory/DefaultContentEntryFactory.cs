using System.Net;
using System.Text;
using Lantern.Discv5.Enr.Content;
using Lantern.Discv5.Enr.Content.EntryTypes;
using Lantern.Discv5.Rlp;

namespace Lantern.Discv5.Enr.Factory;

internal class DefaultContentEntryFactory : IContentEntryFactory
{
    private readonly Dictionary<string, Func<byte[], IContentEntry>> _supportedEntries;

    public DefaultContentEntryFactory()
    {
        _supportedEntries = new Dictionary<string, Func<byte[], IContentEntry>>
        {
            { EnrContentKey.Attnets, value => new EntryAttnets(value) },
            { EnrContentKey.Eth2, value => new EntryEth2(value) },
            { EnrContentKey.Syncnets, value => new EntrySyncnets(Convert.ToHexString(value)) },
            { EnrContentKey.Id, value => new EntryId(Encoding.ASCII.GetString(value)) },
            { EnrContentKey.Ip, value => new EntryIp(new IPAddress(value)) },
            { EnrContentKey.Ip6, value => new EntryIp6(new IPAddress(value)) },
            { EnrContentKey.Secp256K1, value => new EntrySecp256K1(value) },
            { EnrContentKey.Tcp, value => new EntryTcp(RlpExtensions.ByteArrayToInt32(value)) },
            { EnrContentKey.Tcp6, value => new EntryTcp6(RlpExtensions.ByteArrayToInt32(value)) },
            { EnrContentKey.Udp, value => new EntryUdp(RlpExtensions.ByteArrayToInt32(value)) },
            { EnrContentKey.Udp6, value => new EntryUdp6(RlpExtensions.ByteArrayToInt32(value)) },
        };
    }

    public IEnumerable<string> SupportedKeyList => _supportedEntries.Keys;

    public bool TryCreateEntry(string key, byte[] value, out IContentEntry? entry)
    {
        if (_supportedEntries.TryGetValue(key, out var createEntryFunc))
        {
            entry = createEntryFunc(value);
            return true;
        }

        entry = null;
        return false;
    }
}