using System.Text;
using Lantern.Discv5.Enr.Common;
using Lantern.Discv5.Enr.IdentityScheme.Interfaces;
using Lantern.Discv5.Rlp;

namespace Lantern.Discv5.Enr.EnrFactory;

public sealed class EnrRecordFactory
{
    private const int EnrPrefixLength = 4;
    
    public EnrEntryRegistry EntryRegistry { get; } = new();

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
    
    public EnrRecord[] CreateFromMultipleEnrList(IEnumerable<IEnumerable<byte[]>> enrs)
    {
        if (enrs == null) throw new ArgumentNullException(nameof(enrs));
        
        return enrs.Select(enr => CreateFromDecoded(enr.ToArray())).ToArray();
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
            var entry = EntryRegistry.GetEnrEntry(key, items[i + 1]);

            if (entry == null)
                continue;

            enrRecord.SetEntry(key, entry);
        }

        return enrRecord;
    }
}