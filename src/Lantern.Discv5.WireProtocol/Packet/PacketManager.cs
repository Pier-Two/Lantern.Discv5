using System.Net;
using System.Net.Sockets;
using Lantern.Discv5.Enr;
using Lantern.Discv5.Enr.Entries;
using Lantern.Discv5.Rlp;
using Lantern.Discv5.WireProtocol.Connection;
using Lantern.Discv5.WireProtocol.Identity;
using Lantern.Discv5.WireProtocol.Messages;
using Lantern.Discv5.WireProtocol.Packet.Handlers;
using Lantern.Discv5.WireProtocol.Packet.Types;
using Lantern.Discv5.WireProtocol.Session;
using Lantern.Discv5.WireProtocol.Utility;
using Microsoft.Extensions.Logging;

namespace Lantern.Discv5.WireProtocol.Packet;

public class PacketManager(IPacketHandlerFactory packetHandlerFactory,
        IIdentityManager identityManager,
        ISessionManager sessionManager,
        IMessageRequester messageRequester,
        IUdpConnection udpConnection,
        IPacketProcessor packetProcessor,
        IPacketBuilder packetBuilder,
        ILoggerFactory loggerFactory)
    : IPacketManager
{
    private readonly ILogger<PacketManager> _logger = loggerFactory.CreateLogger<PacketManager>();

    public async Task<byte[]?> SendPacket(IEnr dest, MessageType messageType, params byte[][] args)
    {
        var destNodeId = identityManager.Verifier.GetNodeIdFromRecord(dest);
        var destIpKey = dest.GetEntry<EntryIp>(EnrEntryKey.Ip);
        var destUdpKey = dest.GetEntry<EntryUdp>(EnrEntryKey.Udp);

        if (destIpKey == null || destUdpKey == null)
        {
            _logger.LogWarning("No IP or UDP entry in ENR. Cannot send packet");
            return null;
        }

        var destEndPoint = new IPEndPoint(destIpKey.Value, destUdpKey.Value);
        var cryptoSession = sessionManager.GetSession(destNodeId, destEndPoint);
        var sessionEstablished = cryptoSession is { IsEstablished: true };
        var message = ConstructMessage(sessionEstablished, messageType, destNodeId, args);

        if (message == null)
        {
            return null;
        }

        if (sessionEstablished)
        {
            await SendOrdinaryPacketAsync(message, cryptoSession, destEndPoint, destNodeId);
            return message;
        }
        else
        {
            await SendRandomOrdinaryPacketAsync(destEndPoint, destNodeId);
            return message;
        }
    }

    private byte[]? ConstructMessage(bool sessionEstablished, MessageType messageType, byte[] destNodeId, byte[][] args)
    {
        return messageType switch
        {
            MessageType.Ping => sessionEstablished
                ? messageRequester.ConstructPingMessage(destNodeId)
                : messageRequester.ConstructCachedPingMessage(destNodeId),
            MessageType.FindNode => sessionEstablished
                ? messageRequester.ConstructFindNodeMessage(destNodeId, args[0])
                : messageRequester.ConstructCachedFindNodeMessage(destNodeId, args[0]),
            MessageType.TalkReq => sessionEstablished
                ? messageRequester.ConstructTalkReqMessage(destNodeId, args[0], args[1])
                : messageRequester.ConstructCachedTalkReqMessage(destNodeId, args[0], args[1]),
            MessageType.TalkResp => sessionEstablished
                ? messageRequester.ConstructTalkRespMessage(destNodeId, args[0])
                : messageRequester.ConstructCachedTalkRespMessage(destNodeId, args[0]),
            _ => null
        };
    }

    public async Task HandleReceivedPacket(UdpReceiveResult returnedResult)
    {
        try
        {
            var packetHandler =
                packetHandlerFactory.GetPacketHandler(
                    (PacketType)packetProcessor.GetStaticHeader(returnedResult.Buffer).Flag);
            await packetHandler.HandlePacket(returnedResult);
        }
        catch (Exception e)
        {
            _logger.LogDebug("Failed to process the packet received from {RemoteEndPoint}", returnedResult.RemoteEndPoint);
            _logger.LogDebug("Exception: {Exception}", e);
        }
    }

    private async Task SendOrdinaryPacketAsync(byte[] message, ISessionMain sessionMain, IPEndPoint destEndPoint,
        byte[] destNodeId)
    {
        var maskingIv = RandomUtility.GenerateRandomData(PacketConstants.MaskingIvSize);
        var ordinaryPacket = packetBuilder.BuildOrdinaryPacket(message,destNodeId, maskingIv, sessionMain.MessageCount);
        var encryptedMessage = sessionMain.EncryptMessage(ordinaryPacket.Header, maskingIv, message);
        var finalPacket = ByteArrayUtils.JoinByteArrays(ordinaryPacket.Packet, encryptedMessage);

        await udpConnection.SendAsync(finalPacket, destEndPoint);
        _logger.LogInformation("Sent ORDINARY packet to {Destination}", destEndPoint);
    }

    private async Task SendRandomOrdinaryPacketAsync(IPEndPoint destEndPoint, byte[] destNodeId)
    {
        var constructedOrdinaryPacket = packetBuilder.BuildRandomOrdinaryPacket(destNodeId);
        await udpConnection.SendAsync(constructedOrdinaryPacket.Packet, destEndPoint);
        _logger.LogInformation("Sent RANDOM packet to initiate handshake with {Destination}", destEndPoint);
    }
}