using Lantern.Discv5.WireProtocol.Packets.Types;

namespace Lantern.Discv5.WireProtocol.Packets.Handlers;

public interface IPacketHandlerFactory
{
    IPacketHandler GetPacketHandler(PacketType packetType);
}