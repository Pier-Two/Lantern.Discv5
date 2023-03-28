using System.Text;
using Lantern.Discv5.Enr.Content;
using Lantern.Discv5.Rlp;

namespace Lantern.Discv5.Enr.Factory;

public class EnrRecordFactory
{
    private readonly List<IContentEntryFactory> _contentEntryFactories = new();
    private const int EnrPrefixLength = 4;
    
    public EnrRecordFactory()
    {
        _contentEntryFactories.Add(new DefaultContentEntryFactory());
    }
    
    public void RegisterContentEntryFactory(IContentEntryFactory factory)
    {
        _contentEntryFactories.Add(factory);
    }
    
    public EnrRecord[] CreateFromMultipleEnrList(IEnumerable<IEnumerable<byte[]>> enrs)
    {
        return enrs.Select(enr => CreateFromDecoded(enr.ToArray())).ToArray();
    }

    public EnrRecord CreateFromString(string enrString)
    {
        if (enrString.StartsWith("enr:"))
            enrString = enrString[EnrPrefixLength..];

        return CreateFromBytes(Base64Url.FromBase64UrlString(enrString));
    }

    public EnrRecord CreateFromBytes(byte[] bytes)
    {
        var items = RlpDecoder.Decode(bytes);
        return CreateFromDecoded(items);
    }

    public EnrRecord CreateFromDecoded(IReadOnlyList<byte[]> items)
    {
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
    
    private IContentEntry? GetEnrEntry(string stringKey, byte[] value)
    {
        foreach (var factory in _contentEntryFactories)
        {
            if (factory.TryCreateEntry(stringKey, value, out var entry))
            {
                return entry;
            }
        }

        return null;
    }
}