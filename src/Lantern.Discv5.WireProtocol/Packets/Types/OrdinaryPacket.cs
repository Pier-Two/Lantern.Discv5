namespace Lantern.Discv5.WireProtocol.Packets.Types;

public class OrdinaryPacket : Packet
{
    public OrdinaryPacket(byte[] srcNodeId) : base(srcNodeId)
    {
    }
}