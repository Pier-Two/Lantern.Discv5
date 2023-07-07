using Lantern.Discv5.WireProtocol.Packet.Headers;

namespace Lantern.Discv5.WireProtocol.Packet;

public interface IPacketProcessor
{
    StaticHeader GetStaticHeader(byte[] rawPacket);
    
    byte[] GetMaskingIv(byte[] rawPacket);
    
    byte[] GetEncryptedMessage(byte[] rawPacket);
}