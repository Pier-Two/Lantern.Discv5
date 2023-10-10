using Lantern.Discv5.WireProtocol.Identity;
using Lantern.Discv5.WireProtocol.Packet.Headers;
using Lantern.Discv5.WireProtocol.Session;

namespace Lantern.Discv5.WireProtocol.Packet;

public class PacketProcessor(IIdentityManager identityManager, IAesCrypto aesCrypto) : IPacketProcessor
{
    public StaticHeader GetStaticHeader(byte[] rawPacket)
    {
        var decryptedPacket = aesCrypto.AesCtrDecrypt(identityManager.Record.NodeId[..16], rawPacket[..16], rawPacket[16..]);
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