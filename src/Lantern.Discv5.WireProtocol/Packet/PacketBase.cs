namespace Lantern.Discv5.WireProtocol.Packet;

public abstract class PacketBase
{
    protected PacketBase(byte[] authData)
    {
        AuthData = authData;
    }

    public readonly byte[] AuthData;
    
    public int AuthDataSize => AuthData.Length;
}