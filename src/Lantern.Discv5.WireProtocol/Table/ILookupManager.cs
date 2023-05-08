namespace Lantern.Discv5.WireProtocol.Table;

public interface ILookupManager
{
    public Task<List<NodeTableEntry>> PerformLookup(byte[] targetNodeId, int numberOfPaths = 3);
    
    public void ReceiveNodesResponse(List<NodeTableEntry> nodes);
}