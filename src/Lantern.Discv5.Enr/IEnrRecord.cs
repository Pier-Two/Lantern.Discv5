using Lantern.Discv5.Enr.EnrContent;

namespace Lantern.Discv5.Enr;

public interface IEnrRecord
{
    byte[]? Signature { get; }
    
    ulong SequenceNumber { get; }
    
    byte[] NodeId { get; }

    bool HasKey(string key);

    void UpdateEntry<T>(T value) where T : class, IContentEntry;
    
    T GetEntry<T>(string key, T defaultValue = default!) where T : IContentEntry;
    
    byte[] EncodeRecord();
    
    byte[] EncodeContent();
    
    void UpdateSignature();
}