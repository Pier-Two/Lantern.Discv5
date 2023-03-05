namespace Lantern.Discv5.WireProtocol.Packets;

public abstract class Packet
{
    protected Packet(byte[] authData, int authDataSize)
    {
        AuthData = authData;
        AuthDataSize = authDataSize;
    }

    public byte[] AuthData { get; }

    public int AuthDataSize { get; }
}