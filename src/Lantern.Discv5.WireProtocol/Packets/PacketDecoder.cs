using Lantern.Discv5.WireProtocol.Packets.Types;

namespace Lantern.Discv5.WireProtocol.Packets;

public class PacketDecoder
{
    public Packet DecodePacket(PacketType packetType, byte[] authData)
    {
        return packetType switch
        {
            PacketType.Ordinary => new OrdinaryPacket(authData),
            PacketType.WhoAreYou => WhoAreYouPacket.DecodeAuthData(authData),
            PacketType.Handshake => HandshakePacket.DecodeAuthData(authData),
            _ => throw new ArgumentException($"Invalid packet type: {packetType}", nameof(packetType)),
        };
    }

}