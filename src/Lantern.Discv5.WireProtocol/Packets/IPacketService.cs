using System.Net.Sockets;
using Lantern.Discv5.WireProtocol.Connection;

namespace Lantern.Discv5.WireProtocol.Packets;

public interface IPacketService
{
    Task SendOrdinaryPacketForLookup(IUdpConnection udpConnection);
    
    Task HandleReceivedPacket(IUdpConnection connection, UdpReceiveResult returnedResult);
}