using Lantern.Discv5.Enr;
using Lantern.Discv5.WireProtocol.Packet.Headers;

namespace Lantern.Discv5.WireProtocol.Packet;

public interface IPacketBuilder
{
    public Tuple<byte[], StaticHeader> BuildRandomOrdinaryPacket(byte[] destNodeId, byte[] packetNonce, byte[] maskingIv);

    public Tuple<byte[], StaticHeader> BuildOrdinaryPacket(byte[] destNodeId, byte[] maskingIv, byte[] messageCount);

    public Tuple<byte[], StaticHeader> BuildWhoAreYouPacketWithoutEnr(byte[] destNodeId, byte[] packetNonce,
        byte[] maskingIv);

    public Tuple<byte[], StaticHeader> BuildWhoAreYouPacket(byte[] destNodeId, byte[] packetNonce,
        EnrRecord destRecord, byte[] maskingIv);

    public Tuple<byte[], StaticHeader> BuildHandshakePacket(byte[] idSignature, byte[] ephemeralPubKey, byte[] destNodeId, byte[] maskingIv, byte[] messageCount);
}