using System.Net.Sockets;
using Lantern.Discv5.Enr;
using Lantern.Discv5.WireProtocol.Connection;
using Lantern.Discv5.WireProtocol.Message;

namespace Lantern.Discv5.WireProtocol.Packet;

public interface IPacketService
{
    Task InitialiseDiscoveryAsync();
    
    Task HandleReceivedPacket(UdpReceiveResult returnedResult);

    Task PingNodeAsync();
    
    Task SendPacket(MessageType messageType, EnrRecord record);
}