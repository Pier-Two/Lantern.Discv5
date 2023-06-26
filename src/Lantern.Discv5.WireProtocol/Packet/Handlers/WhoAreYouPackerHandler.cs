using System.Net;
using System.Net.Sockets;
using Lantern.Discv5.Rlp;
using Lantern.Discv5.WireProtocol.Connection;
using Lantern.Discv5.WireProtocol.Identity;
using Lantern.Discv5.WireProtocol.Message;
using Lantern.Discv5.WireProtocol.Packet.Types;
using Lantern.Discv5.WireProtocol.Session;
using Lantern.Discv5.WireProtocol.Table;
using Lantern.Discv5.WireProtocol.Utility;
using Microsoft.Extensions.Logging;

namespace Lantern.Discv5.WireProtocol.Packet.Handlers;

public class WhoAreYouPacketHandler : PacketHandlerBase
{
    private readonly IIdentityManager _identityManager;
    private readonly ISessionManager _sessionManager;
    private readonly IRoutingTable _routingTable;
    private readonly IRequestManager _requestManager;
    private readonly IUdpConnection _connection;
    private readonly IAesUtility _aesUtility;
    private readonly IPacketBuilder _packetBuilder;
    private readonly ILogger<WhoAreYouPacketHandler> _logger;

    public WhoAreYouPacketHandler(IIdentityManager identityManager, ISessionManager sessionManager, IRoutingTable routingTable, IRequestManager requestManager, IUdpConnection udpConnection, IAesUtility aesUtility, IPacketBuilder packetBuilder, ILoggerFactory loggerFactory)
    {
        _identityManager = identityManager;
        _sessionManager = sessionManager;
        _routingTable = routingTable;
        _requestManager = requestManager;
        _connection = udpConnection;
        _aesUtility = aesUtility;
        _packetBuilder = packetBuilder;
        _logger = loggerFactory.CreateLogger<WhoAreYouPacketHandler>();
    }

    public override PacketType PacketType => PacketType.WhoAreYou;

    public override async Task HandlePacket(UdpReceiveResult returnedResult)
    {
        _logger.LogInformation("Received WHOAREYOU packet from {Address}", returnedResult.RemoteEndPoint.Address);
        
        var packet = new PacketProcessor(_identityManager, _aesUtility, returnedResult.Buffer);
        var destNodeId = _sessionManager.GetHandshakeInteraction(packet.StaticHeader.Nonce);
        
        if (destNodeId == null)
        {
            _logger.LogWarning("Failed to get dest node id from packet nonce");
            return;
        }
        
        var nodeEntry = _routingTable.GetNodeEntry(destNodeId);
        
        if(nodeEntry == null)
        {
            _logger.LogWarning("Failed to get node entry from the ENR table at node id: {NodeId}", Convert.ToHexString(destNodeId));
            return;
        }
        
        _routingTable.MarkNodeAsLive(nodeEntry.Id);
        var session = GenerateOrUpdateSession(packet, destNodeId, returnedResult.RemoteEndPoint);
        
        if(session == null)
        {
            return;
        }

        var message = CreateReplyMessage(destNodeId);
        
        if(message == null)
        {
            _logger.LogWarning("Failed to construct message in response to WHOAREYOU packet");
            return;
        }
        
        var idSignature = session.GenerateIdSignature(destNodeId);
        
        if(idSignature == null)
        {
            return;
        }
        
        var maskingIv = RandomUtility.GenerateRandomData(PacketConstants.MaskingIvSize);
        var handshakePacket = _packetBuilder.BuildHandshakePacket(idSignature, session.EphemeralPublicKey, destNodeId, maskingIv, session.MessageCount);
        var encryptedMessage = session.EncryptMessageWithNewKeys(nodeEntry.Record, handshakePacket.Item2, _identityManager.NodeId, message, maskingIv);
        var finalPacket = ByteArrayUtils.JoinByteArrays(handshakePacket.Item1, encryptedMessage);
        
        await _connection.SendAsync(finalPacket, returnedResult.RemoteEndPoint);
        _logger.LogInformation("Sent HANDSHAKE packet with encrypted message");
    }

    private SessionMain? GenerateOrUpdateSession(PacketProcessor packet, byte[] destNodeId, IPEndPoint destEndPoint)
    {
        var session = _sessionManager.GetSession(destNodeId, destEndPoint);

        if (session == null)
        {
            _logger.LogDebug("Creating new session with node: {Node}", destEndPoint);
            session = _sessionManager.CreateSession(SessionType.Initiator, destNodeId, destEndPoint);
        }

        if (session != null)
        {
            session.SetChallengeData(packet.MaskingIv, packet.StaticHeader.GetHeader());
            return session;
        }
        
        _logger.LogWarning("Failed to create or update session with node: {Node}", destEndPoint);
        return null;
    }

    private byte[]? CreateReplyMessage(byte[] destNodeId)
    {
        var cachedRequest = _requestManager.GetCachedRequest(destNodeId);
        
        if(cachedRequest == null)
        {
            _logger.LogWarning("Failed to get cached request for node id: {NodeId}", Convert.ToHexString(destNodeId));
            return null;
        }
        
        _requestManager.MarkCachedRequestAsFulfilled(destNodeId);

        _logger.LogInformation("Creating message from cached request {MessageType}", cachedRequest.Message.MessageType);
        
        var pendingRequest = new PendingRequest(cachedRequest.NodeId, cachedRequest.Message);
        _requestManager.AddPendingRequest(cachedRequest.Message.RequestId, pendingRequest);

        return cachedRequest.Message.EncodeMessage();
    }
}