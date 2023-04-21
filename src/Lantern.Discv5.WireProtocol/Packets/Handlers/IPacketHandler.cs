using System.Net.Sockets;
using Lantern.Discv5.WireProtocol.Connection;
using Lantern.Discv5.WireProtocol.Packets.Types;

namespace Lantern.Discv5.WireProtocol.Packets.Handlers;

public interface IPacketHandler
{
    PacketType PacketType { get; }
    
    Task HandlePacket(IUdpConnection connection, UdpReceiveResult returnedResult);
}