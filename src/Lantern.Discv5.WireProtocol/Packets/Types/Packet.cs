namespace Lantern.Discv5.WireProtocol.Packets.Types;

public abstract class Packet
{
    protected Packet(byte[] authData)
    {
        AuthData = authData;
    }

    public readonly byte[] AuthData;
}