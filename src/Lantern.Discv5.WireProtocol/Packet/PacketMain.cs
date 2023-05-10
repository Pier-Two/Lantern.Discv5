using Lantern.Discv5.WireProtocol.Identity;
using Lantern.Discv5.WireProtocol.Packet.Headers;
using Lantern.Discv5.WireProtocol.Session;

namespace Lantern.Discv5.WireProtocol.Packet;

public class PacketMain
{
    private readonly IIdentityManager _identityManager;
    private readonly IAesUtility _aesUtility;
    private readonly byte[] _rawPacket;

    public PacketMain(IIdentityManager identityManager, IAesUtility aesUtility, byte[] rawPacket)
    {
        _identityManager = identityManager;
        _aesUtility = aesUtility;
        _rawPacket = rawPacket;
    }

    public StaticHeader StaticHeader => GetStaticHeader();

    public byte[] MaskingIv => GetMaskingIv();
    
    public byte[] EncryptedMessage => GetEncryptedMessage();

    private StaticHeader GetStaticHeader()
    {
        var decryptedPacket = _aesUtility.AesCtrDecrypt(_identityManager.NodeId[..16], _rawPacket[..16], _rawPacket[16..]);
        var staticHeader = StaticHeader.DecodeFromBytes(decryptedPacket);
        return staticHeader;
    }

    private byte[] GetMaskingIv()
    {
        return _rawPacket.AsSpan()[..16].ToArray();
    }
    
    private byte[] GetEncryptedMessage()
    {
        return _rawPacket[^StaticHeader.EncryptedMessageLength..];
    }
}