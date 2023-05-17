namespace Lantern.Discv5.WireProtocol.Table;

public interface ILookupManager
{
    Task<List<NodeTableEntry>> PerformLookup(byte[] targetNodeId, int numberOfPaths = 3);
    
    void ReceiveNodesResponse(List<NodeTableEntry> nodes);
}