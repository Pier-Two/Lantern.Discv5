using Lantern.Discv5.Rlp;

namespace Lantern.Discv5.Enr;

public record EnrRecord(byte[]? Signature = null)
{
    private readonly Dictionary<string, IContentEntry> _entries = new();
    public ulong SequenceNumber { get; set; }

    public T GetEntry<T>(string key, T defaultValue = default!) where T : IContentEntry
    {
        return _entries.TryGetValue(key, out var value) && value is T result ? result : defaultValue;
    }

    public EnrRecord AddEntry<T>(string key, T value) where T : class, IContentEntry
    {
        if (_entries.ContainsKey(key))
            SequenceNumber++;

        _entries[key] = value;
        return this;
    }

    public byte[] EncodeContent()
    {
        var encodedContent = EncodeEnrContent();
        var encodedSeq = RlpEncoder.EncodeUlong(SequenceNumber);
        var encodedItems = ByteArrayUtils.Concatenate(encodedSeq, encodedContent);
        return RlpEncoder.EncodeCollectionOfBytes(encodedItems);
    }

    public byte[] EncodeEnrRecord()
    {
        if (Signature == null)
            throw new InvalidOperationException("Signature must be set before encoding.");

        var encodedSignature = RlpEncoder.EncodeBytes(Signature);
        var encodedSeq = RlpEncoder.EncodeUlong(SequenceNumber);
        var encodedContent = EncodeEnrContent();
        var encodedItems = ByteArrayUtils.Concatenate(encodedSignature, encodedSeq, encodedContent);
        return RlpEncoder.EncodeCollectionOfBytes(encodedItems);
    }

    public override string ToString()
    {
        return $"enr:{Base64UrlConverter.ToBase64UrlString(EncodeEnrRecord())}";
    }

    private byte[] EncodeEnrContent()
    {
        return _entries
            .OrderBy(e => e.Key)
            .SelectMany(e => e.Value.EncodeEntry())
            .ToArray();
    }
}