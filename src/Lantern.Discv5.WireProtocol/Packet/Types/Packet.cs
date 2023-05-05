namespace Lantern.Discv5.WireProtocol.Packet.Types;

public abstract class Packet
{
    protected Packet(byte[] authData)
    {
        AuthData = authData;
    }

    public readonly byte[] AuthData;
}