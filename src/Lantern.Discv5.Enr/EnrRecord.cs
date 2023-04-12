using Lantern.Discv5.Enr.Common;
using Lantern.Discv5.Enr.EnrContent.Interfaces;
using Lantern.Discv5.Enr.IdentityScheme.Interfaces;
using Lantern.Discv5.Rlp;

namespace Lantern.Discv5.Enr;

public class EnrRecord
{
    private readonly Dictionary<string, IContentEntry> _entries = new();

    public ulong SequenceNumber { get; set; } = 1;
    
    public byte[]? Signature { get; private set; }

    public EnrRecord(byte[]? signature = null)
    {
        Signature = signature;
    }

    public T GetEntry<T>(string key, T defaultValue = default!) where T : IContentEntry
    {
        return _entries.GetValueOrDefault(key) is T result ? result : defaultValue;
    }

    public void SetEntry<T>(string key, T value) where T : class, IContentEntry
    {
        if (_entries.ContainsKey(key))
            SequenceNumber++;

        _entries[key] = value;
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
    
    public void UpdateSignature(IIdentitySchemeSigner signer)
    {
        Signature = signer.SignRecord(this);
    }

    public override string ToString()
    {
        return $"enr:{Base64Url.ToBase64UrlString(EncodeEnrRecord())}";
    }

    private byte[] EncodeEnrContent()
    {
        return _entries
            .OrderBy(e => e.Key)
            .SelectMany(e => e.Value.EncodeEntry())
            .ToArray();
    }
}