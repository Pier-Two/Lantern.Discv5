namespace Lantern.Discv5.WireProtocol.Packet.Types;

public class OrdinaryPacketBase : PacketBase
{
    public OrdinaryPacketBase(byte[] srcNodeId) : base(srcNodeId)
    {
    }
}