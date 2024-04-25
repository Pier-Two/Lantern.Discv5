using Lantern.Discv5.WireProtocol.Table;

namespace Lantern.Discv5.WireProtocol.Messages.Responses;

public class NodesResponseEventArgs(IEnumerable<byte> requestId, IEnumerable<NodeTableEntry> nodes) : EventArgs
{
    public IEnumerable<byte> RequestId { get; } = requestId;
    
    public IEnumerable<NodeTableEntry> Nodes { get; } = nodes;
    
}