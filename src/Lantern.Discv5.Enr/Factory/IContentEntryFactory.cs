using Lantern.Discv5.Enr.Content;

namespace Lantern.Discv5.Enr.Factory;

public interface IContentEntryFactory
{
    IEnumerable<string> SupportedKeyList { get; }
    
    bool TryCreateEntry(string key, byte[] value, out IContentEntry? entry);
}