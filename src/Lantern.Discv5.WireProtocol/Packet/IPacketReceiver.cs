using Lantern.Discv5.Enr;
using Lantern.Discv5.WireProtocol.Messages.Responses;
using Lantern.Discv5.WireProtocol.Table;

namespace Lantern.Discv5.WireProtocol.Packet;

public interface IPacketReceiver
{

    Task<PongMessage?> SendPingAsync(IEnr dest);
    
    Task<IEnr[]?> SendFindNodeAsync(IEnr dest, byte[] targetNodeId);
    
    void RaisePongResponseReceived(PongResponseEventArgs e);
    
    void RaiseNodesResponseReceived(NodesResponseEventArgs e);
}