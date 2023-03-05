namespace Lantern.Discv5.WireProtocol.Packets;

public class OrdinaryPacket : Packet
{
    public OrdinaryPacket(byte[] srcId) : base(srcId, AuthDataSizes.Ordinary)
    {
        SrcId = srcId;
    }

    public byte[] SrcId { get; }
}