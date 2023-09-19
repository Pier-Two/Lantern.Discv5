using System.Net;
using System.Text;
using Lantern.Discv5.Enr.Entries;
using Lantern.Discv5.Rlp;

namespace Lantern.Discv5.Enr;

public sealed class EnrEntryRegistry : IEnrEntryRegistry
{
    private readonly Dictionary<EnrEntryKey, Func<byte[], IEntry>> _registeredEntries = new();
    
    public EnrEntryRegistry() : this(CreateDefaultEntries()) { }
    
    public static EnrEntryRegistry Default { get; } = new();
    
    public EnrEntryRegistry(IEnumerable<(EnrEntryKey, Func<byte[], IEntry>)> entries)
    {
        foreach(var entry in entries)
        {
            _registeredEntries.TryAdd(entry.Item1, entry.Item2);
        }
    }

    public void RegisterEntry(string key, Func<byte[], IEntry> entryCreator)
    {
        _registeredEntries.TryAdd(key, entryCreator);
    }

    public void UnregisterEntry(string key)
    {
        _registeredEntries.Remove(key);
    }

    public IEntry? GetEnrEntry(string stringKey, byte[] value)
    {
        return _registeredEntries.TryGetValue(stringKey, out var createEntryFunc) ? createEntryFunc(value) : null;
    }
    
    private static IEnumerable<(EnrEntryKey, Func<byte[], IEntry>)> CreateDefaultEntries()
    {
        yield return (EnrEntryKey.Attnets, value => new EntryAttnets(value));
        yield return (EnrEntryKey.Eth2, value => new EntryEth2(value));
        yield return (EnrEntryKey.Syncnets, value => new EntrySyncnets(value));
        yield return (EnrEntryKey.Id, value => new EntryId(Encoding.ASCII.GetString(value)));
        yield return (EnrEntryKey.Ip, value => new EntryIp(new IPAddress(value)));
        yield return (EnrEntryKey.Ip6, value => new EntryIp6(new IPAddress(value)));
        yield return (EnrEntryKey.Secp256K1, value => new EntrySecp256K1(value));
        yield return (EnrEntryKey.Tcp, value => new EntryTcp(RlpExtensions.ByteArrayToInt32(value)));
        yield return (EnrEntryKey.Tcp6, value => new EntryTcp6(RlpExtensions.ByteArrayToInt32(value)));
        yield return (EnrEntryKey.Udp, value => new EntryUdp(RlpExtensions.ByteArrayToInt32(value)));
        yield return (EnrEntryKey.Udp6, value => new EntryUdp6(RlpExtensions.ByteArrayToInt32(value)));
    }
}