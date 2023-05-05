namespace Lantern.Discv5.WireProtocol.Packet.Types;

public class OrdinaryPacket : Packet
{
    public OrdinaryPacket(byte[] srcNodeId) : base(srcNodeId)
    {
    }
}