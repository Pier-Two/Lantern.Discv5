using System.Text;
using Lantern.Discv5.Enr.EnrContent;
using Lantern.Discv5.Enr.IdentityScheme.Interfaces;
using Lantern.Discv5.Rlp;

namespace Lantern.Discv5.Enr;

public sealed class EnrRecordFactory : IEnrRecordFactory
{
    public EnrEntryRegistry EntryRegistry { get; } = new();

    public EnrRecord CreateFromString(string enrString, IIdentitySchemeVerifier verifier)
    {
        if (string.IsNullOrEmpty(enrString)) throw new ArgumentNullException(nameof(enrString));
        
        if (enrString.StartsWith("enr:"))
            enrString = enrString[EnrConstants.EnrPrefixLength..];

        return CreateFromBytes(Base64Url.FromBase64UrlString(enrString), verifier);
    }

    public EnrRecord CreateFromBytes(byte[] bytes, IIdentitySchemeVerifier verifier)
    {
        if (bytes == null) throw new ArgumentNullException(nameof(bytes));
        
        var items = RlpDecoder.Decode(bytes);
        return CreateFromDecoded(items, verifier);
    }
    
    public EnrRecord[] CreateFromMultipleEnrList(IEnumerable<IEnumerable<byte[]>> enrs, IIdentitySchemeVerifier verifier)
    {
        if (enrs == null) throw new ArgumentNullException(nameof(enrs));
        
        return enrs.Select(enr => CreateFromDecoded(enr.ToArray(), verifier)).ToArray();
    }

    public EnrRecord CreateFromDecoded(IReadOnlyList<byte[]> items, IIdentitySchemeVerifier verifier)
    {
        if (items == null) throw new ArgumentNullException(nameof(items));
        
        var signature = items[0];
        var entries = new Dictionary<string, IContentEntry>();
        
        for (var i = 2; i < items.Count - 1; i++)
        {
            var key = Encoding.ASCII.GetString(items[i]);
            var entry = EntryRegistry.GetEnrEntry(key, items[i + 1]);

            if (entry == null)
                continue;

            entries.Add(key, entry);
        }
        
        var enrRecord = new EnrRecord(entries, verifier, null, signature, RlpExtensions.ByteArrayToUInt64(items[1]));
        
        return enrRecord;
    }
}