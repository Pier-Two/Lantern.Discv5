namespace Lantern.Discv5.WireProtocol.Packet.Types;

public class OrdinaryPacket : PacketBase
{
    public OrdinaryPacket(byte[] srcNodeId) : base(srcNodeId)
    {
    }
}