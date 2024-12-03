using System.Text;
using Lantern.Discv5.Enr.Identity;
using Lantern.Discv5.Rlp;

namespace Lantern.Discv5.Enr;

public sealed class EnrFactory(IEnrEntryRegistry entryRegistry) : IEnrFactory
{
    public Enr CreateFromString(string enrString, IIdentityVerifier verifier)
    {
        if (string.IsNullOrEmpty(enrString))
            throw new ArgumentNullException(nameof(enrString));

        if (enrString.StartsWith("enr:"))
            enrString = enrString[EnrConstants.EnrPrefixLength..];

        return CreateFromBytes(Base64Url.ToBytes(enrString), verifier);
    }

    public Enr CreateFromBytes(byte[] bytes, IIdentityVerifier verifier)
    {
        if (bytes == null) throw new ArgumentNullException(nameof(bytes));

        var items = RlpDecoder.Decode(bytes);
        return CreateFromDecoded(items, verifier);
    }

    public Enr[] CreateFromMultipleEnrList(IEnumerable<IEnumerable<byte[]>> enrs, IIdentityVerifier verifier)
    {
        if (enrs == null) throw new ArgumentNullException(nameof(enrs));

        return enrs.Select(enr => CreateFromDecoded(enr.ToArray(), verifier)).ToArray();
    }

    public Enr CreateFromDecoded(IReadOnlyList<byte[]> items, IIdentityVerifier verifier)
    {
        if (items == null) throw new ArgumentNullException(nameof(items));

        var signature = items[0];
        var entries = new Dictionary<string, IEntry>();

        for (var i = 2; i < items.Count - 1; i += 2)
        {
            var key = Encoding.ASCII.GetString(items[i]);
            var entry = entryRegistry.GetEnrEntry(key, items[i + 1]);

            if (entry == null)
                continue;

            entries.Add(key, entry);
        }

        var enrRecord = new Enr(entries, verifier, null, signature, RlpExtensions.ByteArrayToUInt64(items[1]));

        return enrRecord;
    }
}