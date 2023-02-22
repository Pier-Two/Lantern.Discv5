using Lantern.Discv5.Rlp;
using NeoSmart.Utils;

namespace Lantern.Discv5.Enr;

public class EnrRecord
{
    private const int RecordPrefixLength = 2;
    private readonly Dictionary<string, IEnrContentEntry> _entries = new();

    public byte[]? Signature { get; set; }

    public ulong SequenceNumber { get; set; }

    public T GetEntry<T>(string key, T defaultValue = default!) where T : IEnrContentEntry
    {
        if (_entries.TryGetValue(key, out var value) && value is T result) return result;

        return defaultValue;
    }

    public void AddEntry<T>(string key, T value) where T : class, IEnrContentEntry
    {
        if (_entries.ContainsKey(key)) SequenceNumber++;

        _entries[key] = value;
    }

    public byte[] EncodeContent()
    {
        var encodedContent = EncodeEnrContent();
        var encodedContentLength = Utility.LengthOf(SequenceNumber) + encodedContent.Length;
        using var stream = new MemoryStream();

        stream.Write(RlpEncoder.GetPayloadPrefix(encodedContentLength));
        stream.Write(RlpExtensions.Encode(SequenceNumber));
        stream.Write(encodedContent);

        return stream.ToArray();
    }

    public byte[] EncodeEnrRecord()
    {
        if (Signature == null) throw new InvalidOperationException("Signature must be set before encoding.");

        var encodedContent = EncodeEnrContent();
        var totalRecordLength = Signature.Length + Utility.LengthOf(SequenceNumber) + encodedContent.Length +
                                RecordPrefixLength;

        using var stream = new MemoryStream();

        stream.Write(RlpEncoder.GetPayloadPrefix(totalRecordLength));
        stream.Write(RlpExtensions.Encode(Signature));
        stream.Write(RlpExtensions.Encode(SequenceNumber));
        stream.Write(encodedContent);

        return stream.ToArray();
    }

    public override string ToString()
    {
        return $"enr:{UrlBase64.Encode(EncodeEnrRecord())}";
    }

    private byte[] EncodeEnrContent()
    {
        var encodedContent = new List<byte[]>();

        foreach (var (_, contentEntry) in _entries.OrderBy(e => e.Key)) encodedContent.Add(contentEntry.EncodeEntry());

        return encodedContent.SelectMany(b => b).ToArray();
    }
}