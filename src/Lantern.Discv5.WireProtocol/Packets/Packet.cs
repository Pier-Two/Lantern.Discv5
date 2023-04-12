using System.Collections.Immutable;

namespace Lantern.Discv5.WireProtocol.Packets;

public abstract class Packet
{
    protected Packet(byte[] authData)
    {
        AuthData = authData;
     
    }

    public readonly byte[] AuthData;
    
    public int AuthDataSize => AuthData.Length;
}