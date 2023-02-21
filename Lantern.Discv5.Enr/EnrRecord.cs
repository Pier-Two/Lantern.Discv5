using Lantern.Discv5.Rlp;
using NeoSmart.Utils;

namespace Lantern.Discv5.Enr;

public class EnrRecord
{
    private readonly SortedDictionary<string, EnrContentEntry> _entries = new();

    public byte[]? Signature { get; set; }

    public ulong SequenceNumber { get; set; }

    public EnrContentEntry? GetEntry(string key)
    {
        return _entries.TryGetValue(key, out var value) ? value : null;
    }

    public void AddEntry(EnrContentEntry entry)
    {
        if (_entries.ContainsKey(entry.Key)) SequenceNumber++;

        _entries[entry.Key] = entry;
    }

    private byte[] GetRlpEncoding()
    {
        var encodedContent = EncodeEnrContent();
        var totalRecordLength = Signature!.Length +
                                Utility.LengthOf(SequenceNumber) +
                                encodedContent.Length + 2;

        using var stream = new MemoryStream();
        stream.Write(RlpEncoder.GetPayloadPrefix(totalRecordLength));
        stream.Write(RlpExtensions.Encode(Signature!));
        stream.Write(RlpExtensions.Encode(SequenceNumber));
        stream.Write(encodedContent);
        stream.Dispose();

        return stream.ToArray();
    }

    public override string ToString()
    {
        return $"enr:{UrlBase64.Encode(GetRlpEncoding().ToArray())}";
    }

    private byte[] EncodeEnrContent()
    {
        var encodedContent = new List<byte[]>();
        foreach (var (_, contentEntry) in _entries.OrderBy(e => e.Key))
            encodedContent.Add(contentEntry.EncodeEntry());
        return encodedContent.SelectMany(b => b).ToArray();
    }
}