using System.Net;
using System.Net.Sockets;
using Lantern.Discv5.Enr;
using Lantern.Discv5.Enr.EnrContent;
using Lantern.Discv5.Enr.EnrContent.Entries;
using Lantern.Discv5.Rlp;
using Lantern.Discv5.WireProtocol.Connection;
using Lantern.Discv5.WireProtocol.Identity;
using Lantern.Discv5.WireProtocol.Message;
using Lantern.Discv5.WireProtocol.Packet.Handlers;
using Lantern.Discv5.WireProtocol.Packet.Types;
using Lantern.Discv5.WireProtocol.Session;
using Lantern.Discv5.WireProtocol.Utility;
using Microsoft.Extensions.Logging;

namespace Lantern.Discv5.WireProtocol.Packet;

public class PacketManager : IPacketManager
{
    private readonly IPacketHandlerFactory _packetHandlerFactory;
    private readonly IIdentityManager _identityManager;
    private readonly ISessionManager _sessionManager;
    private readonly IMessageRequester _messageRequester;
    private readonly IUdpConnection _udpConnection;
    private readonly IPacketProcessor _packetProcessor;
    private readonly IPacketBuilder _packetBuilder;
    private readonly ILogger<PacketManager> _logger;

    public PacketManager(IPacketHandlerFactory packetHandlerFactory, IIdentityManager identityManager,
        ISessionManager sessionManager, IMessageRequester messageRequester, IUdpConnection udpConnection,
        IPacketProcessor packetProcessor, IPacketBuilder packetBuilder, ILoggerFactory loggerFactory)
    {
        _packetHandlerFactory = packetHandlerFactory;
        _identityManager = identityManager;
        _sessionManager = sessionManager;
        _messageRequester = messageRequester;
        _udpConnection = udpConnection;
        _packetProcessor = packetProcessor;
        _packetBuilder = packetBuilder;
        _logger = loggerFactory.CreateLogger<PacketManager>();
    }

    public async Task SendPacket(EnrRecord destRecord, MessageType messageType, params byte[][] args)
    {
        var destNodeId = _identityManager.Verifier.GetNodeIdFromRecord(destRecord);
        var destIpKey = destRecord.GetEntry<EntryIp>(EnrContentKey.Ip);
        var destUdpKey = destRecord.GetEntry<EntryUdp>(EnrContentKey.Udp);

        if (destIpKey == null || destUdpKey == null)
        {
            _logger.LogWarning("No IP or UDP entry in ENR. Cannot send packet");
            return;
        }

        var destEndPoint = new IPEndPoint(destIpKey.Value, destUdpKey.Value);
        var cryptoSession = _sessionManager.GetSession(destNodeId, destEndPoint);
        var sessionEstablished = cryptoSession is { IsEstablished: true };
        var message = ConstructMessage(sessionEstablished, messageType, destNodeId, args);

        if (message == null)
        {
            return;
        }

        if (sessionEstablished)
        {
            await SendOrdinaryPacketAsync(message, cryptoSession, destEndPoint, destNodeId);
        }
        else
        {
            await SendRandomOrdinaryPacketAsync(destEndPoint, destNodeId);
        }
    }

    private byte[]? ConstructMessage(bool sessionEstablished, MessageType messageType, byte[] destNodeId, byte[][] args)
    {
        return messageType switch
        {
            MessageType.Ping => sessionEstablished
                ? _messageRequester.ConstructPingMessage(destNodeId)
                : _messageRequester.ConstructCachedPingMessage(destNodeId),
            MessageType.FindNode => sessionEstablished
                ? _messageRequester.ConstructFindNodeMessage(destNodeId, args[0])
                : _messageRequester.ConstructCachedFindNodeMessage(destNodeId, args[0]),
            MessageType.TalkReq => sessionEstablished
                ? _messageRequester.ConstructTalkReqMessage(destNodeId, args[0], args[1])
                : _messageRequester.ConstructCachedTalkReqMessage(destNodeId, args[0], args[1]),
            MessageType.TalkResp => sessionEstablished
                ? _messageRequester.ConstructTalkRespMessage(destNodeId, args[0])
                : _messageRequester.ConstructCachedTalkRespMessage(destNodeId, args[0]),
            _ => null
        };
    }

    public async Task HandleReceivedPacket(UdpReceiveResult returnedResult)
    {
        try
        {
            var packetHandler =
                _packetHandlerFactory.GetPacketHandler(
                    (PacketType)_packetProcessor.GetStaticHeader(returnedResult.Buffer).Flag);
            await packetHandler.HandlePacket(returnedResult);
        }
        catch (Exception e)
        {
            _logger.LogWarning(
                "An error occurred when trying to handle the received packet. Could not handle as it may have been reordered");
            _logger.LogDebug(e, "Exception details");
        }
    }

    private async Task SendOrdinaryPacketAsync(byte[] message, SessionMain sessionMain, IPEndPoint destEndPoint,
        byte[] destNodeId)
    {
        var maskingIv = RandomUtility.GenerateRandomData(PacketConstants.MaskingIvSize);
        var ordinaryPacket = _packetBuilder.BuildOrdinaryPacket(message,destNodeId, maskingIv, sessionMain.MessageCount);
        var encryptedMessage = sessionMain.EncryptMessage(ordinaryPacket.Item2, maskingIv, message);
        var finalPacket = ByteArrayUtils.JoinByteArrays(ordinaryPacket.Item1, encryptedMessage);

        await _udpConnection.SendAsync(finalPacket, destEndPoint);
        _logger.LogInformation("Sent ORDINARY packet to {Destination}", destEndPoint);
    }

    private async Task SendRandomOrdinaryPacketAsync(IPEndPoint destEndPoint, byte[] destNodeId)
    {
        var constructedOrdinaryPacket = _packetBuilder.BuildRandomOrdinaryPacket(destNodeId);
        await _udpConnection.SendAsync(constructedOrdinaryPacket.Item1, destEndPoint);
        _logger.LogInformation("Sent RANDOM packet to initiate handshake with {Destination}", destEndPoint);
    }
}