using Lantern.Discv5.Enr;
using Lantern.Discv5.Rlp;
using Lantern.Discv5.WireProtocol.Messages;
using Lantern.Discv5.WireProtocol.Packets.Constants;
using Lantern.Discv5.WireProtocol.Packets.Headers;
using Lantern.Discv5.WireProtocol.Packets.Types;
using Lantern.Discv5.WireProtocol.Utility;

namespace Lantern.Discv5.WireProtocol.Packets;

public static class PacketConstructor
{

    /*
    public static async Task<Tuple<byte[], StaticHeader>> ConstructOrdinaryPacket(StaticHeader header, EnrRecord destRecord, byte[] maskingIv)
    {
        
    }*/

    public static async Task<Tuple<byte[], StaticHeader>> ConstructOrdinaryPacket(byte[] srcNodeId, byte[] destNodeId, byte[] packetNonce, byte[] maskingIv)
    {
        var ordinaryPacket = new OrdinaryPacket(srcNodeId);
        var packetStaticHeader = ConstructStaticHeader(PacketType.Ordinary, ordinaryPacket.AuthData, packetNonce);
        var maskedHeader = new MaskedHeader(destNodeId, maskingIv);
        var encryptedMaskedHeader = maskedHeader.GetMaskedHeader(packetStaticHeader.GetHeader());
        var packet = ByteArrayUtils.Concatenate(maskingIv, encryptedMaskedHeader, PacketUtils.GenerateNonce());
        return await Task.FromResult(Tuple.Create(packet, packetStaticHeader));
    }

    public static async Task<Tuple<byte[], StaticHeader>> ConstructWhoAreYouPacket(byte[] destNodeId, byte[] packetNonce, EnrRecord destRecord, byte[] maskingIv)
    {
        var whoAreYouPacket = new WhoAreYouPacket(PacketUtils.GenerateIdNonce(), destRecord.SequenceNumber);
        var packetStaticHeader = ConstructStaticHeader(PacketType.WhoAreYou, whoAreYouPacket.AuthData, packetNonce);
        var maskedHeader = new MaskedHeader(destNodeId, maskingIv);
        var encryptedMaskedHeader = maskedHeader.GetMaskedHeader(packetStaticHeader.GetHeader());
        var packet = ByteArrayUtils.JoinByteArrays(maskingIv, encryptedMaskedHeader);
        return await Task.FromResult(Tuple.Create(packet, packetStaticHeader));
    }
    
    public static async Task<Tuple<byte[], StaticHeader>> ConstructHandshakePacket(byte[] idSignature, byte[] ephemeralPubKey, byte[] selfNodeId, byte[] destNodeId, byte[] maskingIv, EnrRecord selfRecord = null)
    {
        var handshakePacket = new HandshakePacket(idSignature, ephemeralPubKey, selfNodeId, selfRecord.EncodeEnrRecord());
        var packetStaticHeader =
            ConstructStaticHeader(PacketType.Handshake, handshakePacket.AuthData, PacketUtils.GenerateNonce());
        var maskedHeader = new MaskedHeader(destNodeId, maskingIv);
        var encryptedMaskedHeader = maskedHeader.GetMaskedHeader(packetStaticHeader.GetHeader());
        var packet = ByteArrayUtils.Concatenate(maskingIv, encryptedMaskedHeader);
        return await Task.FromResult(Tuple.Create(packet, packetStaticHeader));
    }
    
    private static StaticHeader ConstructStaticHeader(PacketType packetType, byte[] authData, byte[] nonce)
    {
        return new StaticHeader(ProtocolConstants.ProtocolId, ProtocolConstants.Version, authData, (byte)packetType, nonce);
    }

}