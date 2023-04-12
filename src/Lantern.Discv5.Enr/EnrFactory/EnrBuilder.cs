using Lantern.Discv5.Enr.EnrContent.Interfaces;
using Lantern.Discv5.Enr.IdentityScheme.Interfaces;

namespace Lantern.Discv5.Enr.EnrFactory;

public class EnrBuilder
{
    private IIdentitySchemeSigner? _signer;
    private readonly Dictionary<string, IContentEntry> _entries;
    
    public EnrBuilder()
    {
        _entries = new Dictionary<string, IContentEntry>();
    }

    public EnrBuilder WithSigner(IIdentitySchemeSigner signer)
    {
        _signer = signer;
        return this;
    }

    public EnrBuilder WithEntry(string key, IContentEntry entry)
    {
        _entries[key] = entry;
        return this;
    }

    public EnrRecord Build()
    {
        if (_signer == null)
        {
            throw new InvalidOperationException("Signer must be set before building the EnrRecord.");
        }

        var enrRecord = new EnrRecord();

        foreach (var entry in _entries)
        {
            enrRecord.SetEntry(entry.Key, entry.Value);
        }

        enrRecord.UpdateSignature(_signer);

        return enrRecord;
    }
}