using System.Net;
using System.Text;
using Lantern.Discv5.Enr.Content;
using Lantern.Discv5.Enr.Content.EntryTypes;
using Lantern.Discv5.Rlp;

namespace Lantern.Discv5.Enr;

public sealed class EnrRecordFactory
{
    private readonly Dictionary<EnrContentKey, Func<byte[], IContentEntry>> _registeredEntries;
    private const int EnrPrefixLength = 4;
    
    public EnrRecordFactory()
    {
        _registeredEntries = new Dictionary<EnrContentKey, Func<byte[], IContentEntry>>();
        RegisterDefaultEntries();
    }
    
    public void RegisterEntry(string key, Func<byte[], IContentEntry> entryCreator)
    {
        if (!_registeredEntries.TryGetValue(key, out _))
        {
            _registeredEntries.Add(key, entryCreator);
        }
    }
    
    public EnrRecord[] CreateFromMultipleEnrList(IEnumerable<IEnumerable<byte[]>> enrs)
    {
        if (enrs == null) throw new ArgumentNullException(nameof(enrs));
        
        return enrs.Select(enr => CreateFromDecoded(enr.ToArray())).ToArray();
    }

    public EnrRecord CreateFromString(string enrString)
    {
        if (string.IsNullOrEmpty(enrString)) throw new ArgumentNullException(nameof(enrString));
        
        if (enrString.StartsWith("enr:"))
            enrString = enrString[EnrPrefixLength..];

        return CreateFromBytes(Base64Url.FromBase64UrlString(enrString));
    }

    public EnrRecord CreateFromBytes(byte[] bytes)
    {
        if (bytes == null) throw new ArgumentNullException(nameof(bytes));
        
        var items = RlpDecoder.Decode(bytes);
        return CreateFromDecoded(items);
    }

    public EnrRecord CreateFromDecoded(IReadOnlyList<byte[]> items)
    {
        if (items == null) throw new ArgumentNullException(nameof(items));
        
        var signature = items[0];
        var enrRecord = new EnrRecord(signature)
        {
            SequenceNumber = RlpExtensions.ByteArrayToUInt64(items[1])
        };

        for (var i = 2; i < items.Count - 1; i++)
        {
            var key = Encoding.ASCII.GetString(items[i]);
            var entry = GetEnrEntry(key, items[i + 1]);

            if (entry == null)
                continue;

            enrRecord.SetEntry(key, entry);
        }

        return enrRecord;
    }
    
    private void RegisterDefaultEntries()
    {
        RegisterEntry(EnrContentKey.Attnets, value => new EntryAttnets(value));
        RegisterEntry(EnrContentKey.Eth2, value => new EntryEth2(value));
        RegisterEntry(EnrContentKey.Syncnets, value => new EntrySyncnets(Convert.ToHexString(value)));
        RegisterEntry(EnrContentKey.Id, value => new EntryId(Encoding.ASCII.GetString(value)));
        RegisterEntry(EnrContentKey.Ip, value => new EntryIp(new IPAddress(value)));
        RegisterEntry(EnrContentKey.Ip6, value => new EntryIp6(new IPAddress(value)));
        RegisterEntry(EnrContentKey.Secp256K1, value => new EntrySecp256K1(value));
        RegisterEntry(EnrContentKey.Tcp, value => new EntryTcp(RlpExtensions.ByteArrayToInt32(value)));
        RegisterEntry(EnrContentKey.Tcp6, value => new EntryTcp6(RlpExtensions.ByteArrayToInt32(value)));
        RegisterEntry(EnrContentKey.Udp, value => new EntryUdp(RlpExtensions.ByteArrayToInt32(value)));
        RegisterEntry(EnrContentKey.Udp6, value => new EntryUdp6(RlpExtensions.ByteArrayToInt32(value)));
    }

    private IContentEntry? GetEnrEntry(string stringKey, byte[] value)
    {
        if (_registeredEntries.TryGetValue(stringKey, out var createEntryFunc))
        {
            return createEntryFunc(value);
        }

        return null;
    }
}