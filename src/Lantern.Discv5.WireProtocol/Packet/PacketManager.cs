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
    private readonly IAesUtility _aesUtility;
    private readonly IPacketBuilder _packetBuilder;
    private readonly ILogger<PacketManager> _logger;

    public PacketManager(IPacketHandlerFactory packetHandlerFactory, IIdentityManager identityManager,
        ISessionManager sessionManager, IMessageRequester messageRequester, IUdpConnection udpConnection, 
        IAesUtility aesUtility, IPacketBuilder packetBuilder, ILoggerFactory loggerFactory)
    {
        _packetHandlerFactory = packetHandlerFactory;
        _identityManager = identityManager;
        _sessionManager = sessionManager;
        _messageRequester = messageRequester;
        _udpConnection = udpConnection;
        _aesUtility = aesUtility;
        _packetBuilder = packetBuilder;
        _logger = loggerFactory.CreateLogger<PacketManager>();
    }
    
    public async Task HandleReceivedPacket(UdpReceiveResult returnedResult)
    {
        try
        {
            var packet = new PacketProcessor(_identityManager, _aesUtility, returnedResult.Buffer);
            var packetHandler = _packetHandlerFactory.GetPacketHandler((PacketType)packet.StaticHeader.Flag);
            await packetHandler.HandlePacket(returnedResult);
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Error handling received packet");
        }
    }

    private async Task SendOrdinaryPacketAsync(byte[] message, SessionMain sessionMain, IPEndPoint destEndPoint, byte[] destNodeId)
    {
        var maskingIv = RandomUtility.GenerateRandomData(PacketConstants.MaskingIvSize);
        var ordinaryPacket = _packetBuilder.BuildOrdinaryPacket(destNodeId, maskingIv, sessionMain.MessageCount);
        
        var encryptedMessage = sessionMain.EncryptMessage(ordinaryPacket.Item2, maskingIv, message);
        var finalPacket = ByteArrayUtils.JoinByteArrays(ordinaryPacket.Item1, encryptedMessage);
        
        await _udpConnection.SendAsync(finalPacket, destEndPoint);
        _logger.LogInformation("Sent request to {Destination}", destEndPoint);
    }

    private async Task SendRandomOrdinaryPacketAsync(IPEndPoint destEndPoint, byte[] destNodeId)
    {
        var maskingIv = RandomUtility.GenerateRandomData(PacketConstants.MaskingIvSize);
        var packetNonce = RandomUtility.GenerateRandomData(PacketConstants.NonceSize);
            
        _sessionManager.SaveHandshakeInteraction(packetNonce, destNodeId);
            
        var constructedOrdinaryPacket = _packetBuilder.BuildRandomOrdinaryPacket(destNodeId, packetNonce, maskingIv);
        await _udpConnection.SendAsync(constructedOrdinaryPacket.Item1, destEndPoint);
        _logger.LogInformation("Sent RANDOM packet to initiate handshake with {Destination}", destEndPoint);
    }
    
    public async Task SendPacket(EnrRecord destRecord, MessageType messageType, params byte[][] parameters)
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

        if (cryptoSession is { IsEstablished: true })
        {
            var message = messageType switch
            {
                MessageType.Ping => _messageRequester.ConstructPingMessage(destNodeId),
                MessageType.FindNode => _messageRequester.ConstructFindNodeMessage(destNodeId, parameters[0]),
                MessageType.TalkReq => _messageRequester.ConstructTalkReqMessage(destNodeId, parameters[0], parameters[1]),
                MessageType.TalkResp => _messageRequester.ConstructTalkRespMessage(destNodeId, parameters[0]),
                _ => null
            };
            
            if (message == null)
            {
                _logger.LogWarning("Unable to construct message. Cannot send packet");
                return;
            }
            
            await SendOrdinaryPacketAsync(message, cryptoSession, destEndPoint, destNodeId);
        }
        else
        {
            _ = messageType switch
            {
                MessageType.Ping => _messageRequester.ConstructCachedPingMessage(destNodeId),
                MessageType.FindNode => _messageRequester.ConstructCachedFindNodeMessage(destNodeId, parameters[0]),
                MessageType.TalkReq => _messageRequester.ConstructTalkReqMessage(destNodeId, parameters[0], parameters[1]),
                MessageType.TalkResp => _messageRequester.ConstructTalkRespMessage(destNodeId, parameters[0]),
                _ => null
            };

            await SendRandomOrdinaryPacketAsync(destEndPoint, destNodeId);
        }
    }

}