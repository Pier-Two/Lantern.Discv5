using Lantern.Discv5.Enr;
using Lantern.Discv5.Rlp;
using Lantern.Discv5.WireProtocol.Packet.Headers;
using Lantern.Discv5.WireProtocol.Packet.Types;

namespace Lantern.Discv5.WireProtocol.Packet;

public static class PacketConstructor
{
    public static async Task<Tuple<byte[], StaticHeader>> ConstructRandomOrdinaryPacket(byte[] srcNodeId, byte[] destNodeId, byte[] packetNonce, byte[] maskingIv)
    {
        var ordinaryPacket = new OrdinaryPacket(srcNodeId);
        var packetStaticHeader = ConstructStaticHeader(PacketType.Ordinary, ordinaryPacket.AuthData, packetNonce);
        var maskedHeader = new MaskedHeader(destNodeId, maskingIv);
        var encryptedMaskedHeader = maskedHeader.GetMaskedHeader(packetStaticHeader.GetHeader());
        var randomData = RandomUtility.GenerateRandomData(PacketConstants.RandomDataSize);
        var packet = ByteArrayUtils.Concatenate(maskingIv, encryptedMaskedHeader, randomData);
        return await Task.FromResult(Tuple.Create(packet, packetStaticHeader));
    }

    public static async Task<Tuple<byte[], StaticHeader>> ConstructOrdinaryPacket(byte[] srcNodeId, byte[] destNodeId, byte[] maskingIv)
    {
        var ordinaryPacket = new OrdinaryPacket(srcNodeId);
        var packetNonce = RandomUtility.GenerateNonce(PacketConstants.NonceSize);
        var packetStaticHeader = ConstructStaticHeader(PacketType.Ordinary, ordinaryPacket.AuthData, packetNonce);
        var maskedHeader = new MaskedHeader(destNodeId, maskingIv);
        var encryptedMaskedHeader = maskedHeader.GetMaskedHeader(packetStaticHeader.GetHeader());
        var packet = ByteArrayUtils.Concatenate(maskingIv, encryptedMaskedHeader);
        return await Task.FromResult(Tuple.Create(packet, packetStaticHeader));
    }
    
    public static async Task<Tuple<byte[], StaticHeader>> ConstructWhoAreYouPacketWithoutEnr(byte[] destNodeId, byte[] packetNonce, byte[] maskingIv)
    {
        var whoAreYouPacket = new WhoAreYouPacket(RandomUtility.GenerateIdNonce(PacketConstants.IdNonceSize), 0);
        var packetStaticHeader = ConstructStaticHeader(PacketType.WhoAreYou, whoAreYouPacket.AuthData, packetNonce);
        var maskedHeader = new MaskedHeader(destNodeId, maskingIv);
        var encryptedMaskedHeader = maskedHeader.GetMaskedHeader(packetStaticHeader.GetHeader());
        var packet = ByteArrayUtils.JoinByteArrays(maskingIv, encryptedMaskedHeader);
        return await Task.FromResult(Tuple.Create(packet, packetStaticHeader));
    }

    public static async Task<Tuple<byte[], StaticHeader>> ConstructWhoAreYouPacket(byte[] destNodeId, byte[] packetNonce, EnrRecord destRecord, byte[] maskingIv)
    {
        var whoAreYouPacket = new WhoAreYouPacket(RandomUtility.GenerateIdNonce(PacketConstants.IdNonceSize), destRecord.SequenceNumber);
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
            ConstructStaticHeader(PacketType.Handshake, handshakePacket.AuthData, RandomUtility.GenerateNonce(PacketConstants.NonceSize));
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