using System.Net;
using System.Net.Sockets;
using Lantern.Discv5.Rlp;
using Lantern.Discv5.WireProtocol.Connection;
using Lantern.Discv5.WireProtocol.Identity;
using Lantern.Discv5.WireProtocol.Message;
using Lantern.Discv5.WireProtocol.Packet.Headers;
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
    private readonly IPacketBuilder _packetBuilder;
    private readonly IPacketProcessor _packetProcessor;
    private readonly ILogger<WhoAreYouPacketHandler> _logger;

    public WhoAreYouPacketHandler(IIdentityManager identityManager, ISessionManager sessionManager, 
        IRoutingTable routingTable, IRequestManager requestManager, IUdpConnection udpConnection, 
        IPacketBuilder packetBuilder, IPacketProcessor packetProcessor, ILoggerFactory loggerFactory)
    {
        _identityManager = identityManager;
        _sessionManager = sessionManager;
        _routingTable = routingTable;
        _requestManager = requestManager;
        _connection = udpConnection;
        _packetBuilder = packetBuilder;
        _packetProcessor = packetProcessor;
        _logger = loggerFactory.CreateLogger<WhoAreYouPacketHandler>();
    }

    public override PacketType PacketType => PacketType.WhoAreYou;

    public override async Task HandlePacket(UdpReceiveResult returnedResult)
    {
        _logger.LogInformation("Received WHOAREYOU packet from {Address}", returnedResult.RemoteEndPoint.Address);
        
        var packet = returnedResult.Buffer;
        var destNodeId = _requestManager.GetCachedHandshakeInteraction(_packetProcessor.GetStaticHeader(packet).Nonce);
        
        if (destNodeId == null)
        {
            _logger.LogWarning("Failed to get dest node id from packet nonce. Ignoring WHOAREYOU request");
            return;
        }
        
        var nodeEntry = _routingTable.GetNodeEntry(destNodeId);
        
        if(nodeEntry == null)
        {
            _logger.LogWarning("Failed to get node entry from the ENR table at node id: {NodeId}", Convert.ToHexString(destNodeId));
            return;
        }
        
        var session = GenerateOrUpdateSession(_packetProcessor.GetStaticHeader(packet), _packetProcessor.GetMaskingIv(packet), destNodeId, returnedResult.RemoteEndPoint);
        
        if(session == null)
        {
            return;
        }

        var message = CreateReplyMessage(destNodeId);
        
        if(message == null)
        {
            _logger.LogWarning("Failed to construct message in response to WHOAREYOU packet. Sending RANDOM packet");
            await SendRandomOrdinaryPacketAsync(returnedResult.RemoteEndPoint, destNodeId, _connection);
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
        
        if(encryptedMessage == null)
        {
            _logger.LogWarning("Failed to encrypt message with new keys");
            return;
        }
        
        var finalPacket = ByteArrayUtils.JoinByteArrays(handshakePacket.Item1, encryptedMessage);
        
        await _connection.SendAsync(finalPacket, returnedResult.RemoteEndPoint);
        _logger.LogInformation("Sent HANDSHAKE packet to {RemoteEndPoint}", returnedResult.RemoteEndPoint);
    }

    private ISessionMain? GenerateOrUpdateSession(StaticHeader header,byte[] maskingIv, byte[] destNodeId, IPEndPoint destEndPoint)
    {
        var session = _sessionManager.GetSession(destNodeId, destEndPoint);

        if (session == null)
        {
            _logger.LogDebug("Creating new session with node: {Node}", destEndPoint);
            session = _sessionManager.CreateSession(SessionType.Initiator, destNodeId, destEndPoint);
        }

        if (session != null)
        {
            session.SetChallengeData(maskingIv, header.GetHeader());
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
            var existingRequest = _requestManager.GetPendingRequestByNodeId(destNodeId);
            
            if(existingRequest == null)
            {
                _logger.LogWarning("No cached or pending request found for node {NodeId}", Convert.ToHexString(destNodeId));
                return null;
            }

            var newRequest = new PendingRequest(destNodeId, existingRequest.Message);
            
            _requestManager.AddPendingRequest(existingRequest.Message.RequestId, newRequest);
            
            return existingRequest.Message.EncodeMessage();
        }
        
        _requestManager.MarkCachedRequestAsFulfilled(destNodeId);
        _logger.LogInformation("Creating message from cached request {MessageType}", cachedRequest.Message.MessageType);
        
        var pendingRequest = new PendingRequest(cachedRequest.NodeId, cachedRequest.Message);
        _requestManager.AddPendingRequest(cachedRequest.Message.RequestId, pendingRequest);

        return cachedRequest.Message.EncodeMessage();
    }
    
    private async Task SendRandomOrdinaryPacketAsync(IPEndPoint destEndPoint, byte[] destNodeId, IUdpConnection connection)
    {
        var constructedOrdinaryPacket = _packetBuilder.BuildRandomOrdinaryPacket(destNodeId);
        await connection.SendAsync(constructedOrdinaryPacket.Item1, destEndPoint);
        _logger.LogInformation("Sent RANDOM packet to initiate handshake with {Destination}", destEndPoint);
    }
}