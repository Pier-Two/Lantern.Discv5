using Lantern.Discv5.Enr.EnrContent;
using Lantern.Discv5.Enr.EnrContent.Entries;
using Lantern.Discv5.Enr.IdentityScheme.Interfaces;
using Lantern.Discv5.Rlp;
using Multiformats.Base;
using Multiformats.Hash;

namespace Lantern.Discv5.Enr;

public class EnrRecord : IEnrRecord
{
    private readonly Dictionary<string, IContentEntry> _entries;
    private readonly IIdentitySchemeSigner? _signer;
    private readonly IIdentitySchemeVerifier? _verifier;

    public EnrRecord(
        IDictionary<string, IContentEntry> initialEntries, 
        IIdentitySchemeVerifier verifier, 
        IIdentitySchemeSigner? signer = null, 
        byte[]? signature = null, 
        ulong sequenceNumber = 1)
    {
        _entries = InitialiseEntries(initialEntries);
        _verifier = verifier ?? throw new ArgumentNullException(nameof(verifier));
        _signer = signer;

        if (_signer != null)
        {
            Signature = _signer.SignRecord(this);
        }
        else if (signature != null)
        {
            Signature = signature;
        }
        else
        {
            throw new ArgumentNullException($"You must provide either {nameof(signer)} or {nameof(signature)}");
        }        

        SequenceNumber = sequenceNumber;
    }
    
    public byte[]? Signature { get; private set; }
    
    public ulong SequenceNumber { get; private set; }
    
    public byte[] NodeId => _verifier!.GetNodeIdFromRecord(this);
    
    public T GetEntry<T>(string key, T defaultValue = default!) where T : IContentEntry
    {
        var entry = _entries.Values.FirstOrDefault(e => e.Key == key);

        return entry is T result ? result : defaultValue;
    }
    
    public void UpdateEntry<T>(T value) where T : class, IContentEntry
    {
        foreach (var existingKey in _entries.Where(entry => entry.Value.Key.Equals(value.Key)).ToList())
        {
            _entries.Remove(existingKey.Key);
        }
        
        _entries[value.Key] = value;
        IncrementSequenceNumber();
    }
    
    public bool HasKey(string key)
    {
        return _entries.ContainsKey(key);
    }
    
    public void UpdateSignature()
    {
        if(_signer != null)
            Signature = _signer.SignRecord(this);
    }

    public byte[] EncodeContent()
    {
        var encodedContent = EncodeEnrContent();
        var encodedSeq = RlpEncoder.EncodeUlong(SequenceNumber);
        var encodedItems = ByteArrayUtils.Concatenate(encodedSeq, encodedContent);
        return RlpEncoder.EncodeCollectionOfBytes(encodedItems);
    }

    public byte[] EncodeRecord()
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
        return $"enr:{Base64Url.ToBase64UrlString(EncodeRecord())}";
    }

    public string ToPeerId()
    {
        var publicKey = GetEntry<EntrySecp256K1>(EnrContentKey.Secp256K1).Value;
        var publicKeyProto = ByteArrayUtils.Concatenate(EnrConstants.ProtoBufferPrefix, publicKey);
        var multihash = publicKeyProto.Length <= 42 ? Multihash.Encode(publicKeyProto, HashType.ID) : Multihash.Encode(publicKeyProto, HashType.SHA2_256);
    
        return Multibase.Encode(MultibaseEncoding.Base58Btc, multihash).Remove(0, 1);
    }
    
    private static Dictionary<string, IContentEntry> InitialiseEntries(IDictionary<string, IContentEntry> initialEntries)
    {
        var entries = new Dictionary<string, IContentEntry>();
        
        foreach (var entry in initialEntries)
        {
            entries[entry.Key] = entry.Value;
        }
        
        return entries;
    }
    
    private void IncrementSequenceNumber()
    {
        SequenceNumber++;

        if (_signer != null)
            UpdateSignature();
    }

    private byte[] EncodeEnrContent()
    {
        return _entries
            .OrderBy(e => e.Key)
            .SelectMany(e => e.Value.EncodeEntry())
            .ToArray();
    }
}