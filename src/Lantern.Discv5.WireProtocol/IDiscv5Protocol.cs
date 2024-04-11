using Lantern.Discv5.Enr;
using Lantern.Discv5.WireProtocol.Message.Responses;

namespace Lantern.Discv5.WireProtocol;

public interface IDiscv5Protocol
{
    IEnr SelfEnr { get; }
    
    IEnumerable<IEnr> GetActiveNodes { get; }
    
    IEnumerable<IEnr> GetAllNodes { get; }
    
    Task<bool> InitAsync();
    
    Task<IEnumerable<IEnr>?> DiscoverAsync(byte[] targetNodeId);

    Task<PongMessage?> SendPingAsync(IEnr destination);

    Task<IEnumerable<IEnr>?> SendFindNodeAsync(IEnr destination, byte[] targetNodeId);

    Task<bool> SendTalkReqAsync(IEnr destination, byte[] protocol, byte[] request);
    
    Task StopAsync();
}