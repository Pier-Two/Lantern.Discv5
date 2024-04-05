using Lantern.Discv5.Enr;

namespace Lantern.Discv5.WireProtocol;

public interface IDiscv5Protocol
{
    IEnr SelfEnr { get; }
    
    IEnumerable<IEnr> GetActiveNodes { get; }
    
    IEnumerable<IEnr> GetAllNodes { get; }
    
    Task InitAsync();
    
    Task<IEnumerable<IEnr>?> PerformLookupAsync(byte[] targetNodeId);
    
    Task StopAsync();
}