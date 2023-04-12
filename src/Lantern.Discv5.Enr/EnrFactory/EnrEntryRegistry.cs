using System.Net;
using System.Text;
using Lantern.Discv5.Enr.EnrContent;
using Lantern.Discv5.Enr.EnrContent.Entries;
using Lantern.Discv5.Enr.EnrContent.Interfaces;
using Lantern.Discv5.Rlp;

namespace Lantern.Discv5.Enr.EnrFactory;

public sealed class EnrEntryRegistry
{
    private readonly Dictionary<EnrContentKey, Func<byte[], IContentEntry>> _registeredEntries = new();

    public EnrEntryRegistry()
    {
        RegisterDefaultEntries();
    }

    public void RegisterEntry(string key, Func<byte[], IContentEntry> entryCreator)
    {
        _registeredEntries.TryAdd(key, entryCreator);
    }

    public void UnregisterEntry(string key)
    {
        _registeredEntries.Remove(key);
    }

    public IContentEntry? GetEnrEntry(string stringKey, byte[] value)
    {
        return _registeredEntries.TryGetValue(stringKey, out var createEntryFunc) ? createEntryFunc(value) : null;
    }
    
    private void RegisterDefaultEntries()
    {
        RegisterEntry(EnrContentKey.Attnets, value => new EntryAttnets(value));
        RegisterEntry(EnrContentKey.Eth2, value => new EntryEth2(value));
        RegisterEntry(EnrContentKey.Syncnets, value => new EntrySyncnets(value));
        RegisterEntry(EnrContentKey.Id, value => new EntryId(Encoding.ASCII.GetString(value)));
        RegisterEntry(EnrContentKey.Ip, value => new EntryIp(new IPAddress(value)));
        RegisterEntry(EnrContentKey.Ip6, value => new EntryIp6(new IPAddress(value)));
        RegisterEntry(EnrContentKey.Secp256K1, value => new EntrySecp256K1(value));
        RegisterEntry(EnrContentKey.Tcp, value => new EntryTcp(RlpExtensions.ByteArrayToInt32(value)));
        RegisterEntry(EnrContentKey.Tcp6, value => new EntryTcp6(RlpExtensions.ByteArrayToInt32(value)));
        RegisterEntry(EnrContentKey.Udp, value => new EntryUdp(RlpExtensions.ByteArrayToInt32(value)));
        RegisterEntry(EnrContentKey.Udp6, value => new EntryUdp6(RlpExtensions.ByteArrayToInt32(value)));
    }
}