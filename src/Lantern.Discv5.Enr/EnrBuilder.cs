using Lantern.Discv5.Enr.EnrContent;
using Lantern.Discv5.Enr.IdentityScheme.Interfaces;

namespace Lantern.Discv5.Enr;

public class EnrBuilder
{
    private readonly IIdentitySchemeVerifier _verifier;
    private readonly IIdentitySchemeSigner _signer;
    private readonly Dictionary<string, IContentEntry> _entries = new();
    
    public EnrBuilder(IIdentitySchemeVerifier verifier, IIdentitySchemeSigner signer)
    {
        _verifier = verifier;
        _signer = signer;
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

        var enrRecord = new EnrRecord(_entries,_verifier, _signer);
        
        enrRecord.UpdateSignature();
        
        return enrRecord;
    }
}