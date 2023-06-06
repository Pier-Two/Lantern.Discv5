namespace Lantern.Discv5.WireProtocol.Packet.Types;

public abstract class PacketBase
{
    protected PacketBase(byte[] authData)
    {
        AuthData = authData;
    }

    public byte[] AuthData { get; }
}