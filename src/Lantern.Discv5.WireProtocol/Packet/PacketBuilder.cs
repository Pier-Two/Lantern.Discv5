using Lantern.Discv5.Enr;
using Lantern.Discv5.Rlp;
using Lantern.Discv5.WireProtocol.Identity;
using Lantern.Discv5.WireProtocol.Message;
using Lantern.Discv5.WireProtocol.Packet.Headers;
using Lantern.Discv5.WireProtocol.Packet.Types;
using Lantern.Discv5.WireProtocol.Session;
using Lantern.Discv5.WireProtocol.Utility;

namespace Lantern.Discv5.WireProtocol.Packet;

public class PacketBuilder : IPacketBuilder
{
    private readonly IIdentityManager _identityManager;
    private readonly IAesUtility _aesUtility;
    private readonly IRequestManager _requestManager;

    public PacketBuilder(IIdentityManager identityManager, IAesUtility aesUtility, IRequestManager requestManager)
    {
        _identityManager = identityManager;
        _aesUtility = aesUtility;
        _requestManager = requestManager;
    }

    public Tuple<byte[], StaticHeader> BuildRandomOrdinaryPacket(byte[] destNodeId)
    {
        var maskingIv = RandomUtility.GenerateRandomData(PacketConstants.MaskingIvSize);
        var packetNonce = RandomUtility.GenerateRandomData(PacketConstants.NonceSize);
        
        _requestManager.AddCachedHandshakeInteraction(packetNonce, destNodeId);
        
        var ordinaryPacket = new OrdinaryPacketBase(_identityManager.NodeId);
        var packetStaticHeader = ConstructStaticHeader(PacketType.Ordinary, ordinaryPacket.AuthData, packetNonce);
        var maskedHeader = new MaskedHeader(destNodeId, maskingIv);
        var encryptedMaskedHeader = maskedHeader.GetMaskedHeader(packetStaticHeader.GetHeader(), _aesUtility);
        var randomData = RandomUtility.GenerateRandomData(PacketConstants.RandomDataSize);
        var packet = ByteArrayUtils.Concatenate(maskingIv, encryptedMaskedHeader, randomData);
        
        return Tuple.Create(packet, packetStaticHeader);
    }

    public Tuple<byte[], StaticHeader> BuildOrdinaryPacket(byte[] message, byte[] destNodeId, byte[] maskingIv, byte[] messageCount)
    {
        var ordinaryPacket = new OrdinaryPacketBase(_identityManager.NodeId);
        var packetNonce = ByteArrayUtils.JoinByteArrays(messageCount, RandomUtility.GenerateRandomData(PacketConstants.PartialNonceSize));
        var cachedRequest = new CachedRequest(destNodeId, new MessageDecoder().DecodeMessage(message)); 
        
        _requestManager.AddCachedHandshakeInteraction(packetNonce, destNodeId);
        _requestManager.AddCachedRequest(packetNonce, cachedRequest);
        
        var packetStaticHeader = ConstructStaticHeader(PacketType.Ordinary, ordinaryPacket.AuthData, packetNonce);
        var maskedHeader = new MaskedHeader(destNodeId, maskingIv);
        var encryptedMaskedHeader = maskedHeader.GetMaskedHeader(packetStaticHeader.GetHeader(), _aesUtility);
        var packet = ByteArrayUtils.Concatenate(maskingIv, encryptedMaskedHeader);
        
        return Tuple.Create(packet, packetStaticHeader);
    }
    
    public Tuple<byte[], StaticHeader> BuildWhoAreYouPacketWithoutEnr(byte[] destNodeId, byte[] packetNonce, byte[] maskingIv)
    {
        var whoAreYouPacket = new WhoAreYouPacketBase(RandomUtility.GenerateRandomData(PacketConstants.IdNonceSize), 0);
        var packetStaticHeader = ConstructStaticHeader(PacketType.WhoAreYou, whoAreYouPacket.AuthData, packetNonce);
        var maskedHeader = new MaskedHeader(destNodeId, maskingIv);
        var encryptedMaskedHeader = maskedHeader.GetMaskedHeader(packetStaticHeader.GetHeader(), _aesUtility);
        var packet = ByteArrayUtils.JoinByteArrays(maskingIv, encryptedMaskedHeader);
         
        return Tuple.Create(packet, packetStaticHeader);
    }

    public Tuple<byte[], StaticHeader> BuildWhoAreYouPacket(byte[] destNodeId, byte[] packetNonce, EnrRecord destRecord, byte[] maskingIv)
    {
        var whoAreYouPacket = new WhoAreYouPacketBase(RandomUtility.GenerateRandomData(PacketConstants.IdNonceSize), destRecord.SequenceNumber);
        var packetStaticHeader = ConstructStaticHeader(PacketType.WhoAreYou, whoAreYouPacket.AuthData, packetNonce);
        var maskedHeader = new MaskedHeader(destNodeId, maskingIv);
        var encryptedMaskedHeader = maskedHeader.GetMaskedHeader(packetStaticHeader.GetHeader(), _aesUtility);
        var packet = ByteArrayUtils.JoinByteArrays(maskingIv, encryptedMaskedHeader);
        
        return Tuple.Create(packet, packetStaticHeader);
    }
    
    public Tuple<byte[], StaticHeader> BuildHandshakePacket(byte[] idSignature, byte[] ephemeralPubKey, byte[] destNodeId, byte[] maskingIv, byte[] messageCount)
    {
        var handshakePacket = new HandshakePacketBase(idSignature, ephemeralPubKey,_identityManager.NodeId, _identityManager.Record.EncodeEnrRecord());
        var packetNonce = ByteArrayUtils.JoinByteArrays(messageCount, RandomUtility.GenerateRandomData(PacketConstants.PartialNonceSize));
        var packetStaticHeader =
            ConstructStaticHeader(PacketType.Handshake, handshakePacket.AuthData, packetNonce);
        var maskedHeader = new MaskedHeader(destNodeId, maskingIv);
        var encryptedMaskedHeader = maskedHeader.GetMaskedHeader(packetStaticHeader.GetHeader(), _aesUtility);
        var packet = ByteArrayUtils.Concatenate(maskingIv, encryptedMaskedHeader);
        
        return Tuple.Create(packet, packetStaticHeader);
    }
    
    private static StaticHeader ConstructStaticHeader(PacketType packetType, byte[] authData, byte[] nonce)
    {
        return new StaticHeader(ProtocolConstants.ProtocolId, ProtocolConstants.Version, authData, (byte)packetType, nonce);
    }
}