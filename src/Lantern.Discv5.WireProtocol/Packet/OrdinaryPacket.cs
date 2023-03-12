using Lantern.Discv5.Rlp;

namespace Lantern.Discv5.WireProtocol.Packet;

public class OrdinaryPacket : PacketBase
{
    public OrdinaryPacket(byte[] srcNodeId) : base(srcNodeId)
    {
    }
}