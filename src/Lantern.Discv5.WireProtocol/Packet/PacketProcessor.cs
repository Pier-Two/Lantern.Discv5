using Lantern.Discv5.WireProtocol.Identity;
using Lantern.Discv5.WireProtocol.Packet.Headers;
using Lantern.Discv5.WireProtocol.Session;

namespace Lantern.Discv5.WireProtocol.Packet;

public class PacketProcessor : IPacketProcessor
{
    private readonly IIdentityManager _identityManager;
    private readonly IAesCrypto _aesCrypto;
    
    public PacketProcessor(IIdentityManager identityManager, IAesCrypto aesCrypto)
    {
        _identityManager = identityManager;
        _aesCrypto = aesCrypto;
    }

    public StaticHeader GetStaticHeader(byte[] rawPacket)
    {
        var decryptedPacket = _aesCrypto.AesCtrDecrypt(_identityManager.NodeId[..16], rawPacket[..16], rawPacket[16..]);
        var staticHeader = StaticHeader.DecodeFromBytes(decryptedPacket);
        
        return staticHeader;
    }
    
    public byte[] GetMaskingIv(byte[] rawPacket)
    {
        return rawPacket.AsSpan()[..16].ToArray();
    }
    
    public byte[] GetEncryptedMessage(byte[] rawPacket)
    {
        var staticHeader = GetStaticHeader(rawPacket);
        return rawPacket[^staticHeader.EncryptedMessageLength..];
    }
}