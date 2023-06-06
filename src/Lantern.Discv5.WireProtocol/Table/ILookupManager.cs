namespace Lantern.Discv5.WireProtocol.Table;

public interface ILookupManager
{
    Task<List<NodeTableEntry>?> LookupAsync(byte[] targetNodeId);
    
    Task StartLookupAsync(byte[] targetNodeId);
    
    Task ContinueLookupAsync(List<NodeTableEntry> nodes, byte[] senderNode, int expectedResponses);

    bool IsLookupInProgress { get; }
}