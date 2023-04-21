using System.Net.Sockets;
using Lantern.Discv5.WireProtocol.Connection;
using Lantern.Discv5.WireProtocol.Packets.Types;

namespace Lantern.Discv5.WireProtocol.Packets.Handlers;

public abstract class PacketHandlerBase : IPacketHandler
{
    public abstract PacketType PacketType { get; }
    
    public abstract Task HandlePacket(IUdpConnection connection, UdpReceiveResult returnedResult);
}