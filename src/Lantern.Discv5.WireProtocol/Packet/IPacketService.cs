using System.Net.Sockets;
using Lantern.Discv5.WireProtocol.Connection;

namespace Lantern.Discv5.WireProtocol.Packet;

public interface IPacketService
{
    Task RunDiscoveryAsync();
    
    Task HandleReceivedPacket(UdpReceiveResult returnedResult);
}