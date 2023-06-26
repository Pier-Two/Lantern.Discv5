using System.Net;
using System.Net.Sockets;
using Lantern.Discv5.Enr;
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

public class OrdinaryPacketHandler : PacketHandlerBase
{
    private readonly IIdentityManager _identityManager;
    private readonly ISessionManager _sessionManager;
    private readonly IRoutingTable _routingTable;
    private readonly IMessageResponder _messageResponder;
    private readonly IUdpConnection _connection;
    private readonly IAesUtility _aesUtility;
    private readonly IPacketBuilder _packetBuilder;
    private readonly ILogger<OrdinaryPacketHandler> _logger;

    public OrdinaryPacketHandler(IIdentityManager identityManager, ISessionManager sessionManager,
        IRoutingTable routingTable, IMessageResponder messageResponder, IUdpConnection udpConnection,
        IAesUtility aesUtility, IPacketBuilder packetBuilder, ILoggerFactory loggerFactory)
    {
        _identityManager = identityManager;
        _sessionManager = sessionManager;
        _routingTable = routingTable;
        _messageResponder = messageResponder;
        _connection = udpConnection;
        _aesUtility = aesUtility;
        _packetBuilder = packetBuilder;
        _logger = loggerFactory.CreateLogger<OrdinaryPacketHandler>();
    }

    public override PacketType PacketType => PacketType.Ordinary;

    public override async Task HandlePacket(UdpReceiveResult returnedResult)
    {
        _logger.LogInformation("Received ORDINARY packet from {Address}", returnedResult.RemoteEndPoint.Address);
        var packet = new PacketProcessor(_identityManager, _aesUtility, returnedResult.Buffer);
        var nodeEntry = _routingTable.GetNodeEntry(packet.StaticHeader.AuthData);
        
        if(nodeEntry == null)
        {
            await SendWhoAreYouPacketWithoutEnrAsync(packet, returnedResult.RemoteEndPoint, _connection);
            return;
        }
        
        var session = _sessionManager.GetSession(packet.StaticHeader.AuthData, returnedResult.RemoteEndPoint);
        
        if (session == null)
        {
            await SendWhoAreYouPacketAsync(packet, nodeEntry.Record, returnedResult.RemoteEndPoint, _connection);
            return;
        }

        var decryptedMessage = session.DecryptMessage(packet);

        if (decryptedMessage == null)
        {
            await SendWhoAreYouPacketAsync(packet, nodeEntry.Record, returnedResult.RemoteEndPoint, _connection);
            return;
        }
        
        _logger.LogDebug("Successfully decrypted ORDINARY packet");
                    
        var response = await _messageResponder.HandleMessage(decryptedMessage, returnedResult.RemoteEndPoint);

        if (response != null)
        {
            await SendResponseToOrdinaryPacketAsync(packet, session, returnedResult.RemoteEndPoint, _connection,
                response);
        }
    }

    private async Task SendWhoAreYouPacketWithoutEnrAsync(PacketProcessor packet, IPEndPoint destEndPoint, IUdpConnection connection)
    {
        _logger.LogWarning("Could not find record in the table for node: {NodeId}", Convert.ToHexString(packet.StaticHeader.AuthData));
        
        var maskingIv = RandomUtility.GenerateRandomData(PacketConstants.MaskingIvSize);
        var whoAreYouPacket = _packetBuilder.BuildWhoAreYouPacketWithoutEnr(packet.StaticHeader.AuthData, packet.StaticHeader.Nonce, maskingIv);

        _sessionManager.CreateSession(SessionType.Recipient, packet.StaticHeader.AuthData, destEndPoint);
        
        var session = _sessionManager.GetSession(packet.StaticHeader.AuthData, destEndPoint);
        session!.SetChallengeData(maskingIv, whoAreYouPacket.Item2.GetHeader()); 
        
        await connection.SendAsync(whoAreYouPacket.Item1, destEndPoint);
        _logger.LogInformation("Sent WHOAREYOU packet to {RemoteEndPoint}", destEndPoint);
    }

    private async Task SendWhoAreYouPacketAsync(PacketProcessor packet, EnrRecord destNodeRecord, IPEndPoint destEndPoint, IUdpConnection connection)
    {
        _logger.LogWarning("Cannot decrypt ORDINARY packet. No sessionMain found");
        
        var maskingIv = RandomUtility.GenerateRandomData(PacketConstants.MaskingIvSize);
        var constructedWhoAreYouPacket = _packetBuilder.BuildWhoAreYouPacket(packet.StaticHeader.AuthData, packet.StaticHeader.Nonce, destNodeRecord, maskingIv);
       
        _sessionManager.CreateSession(SessionType.Recipient, packet.StaticHeader.AuthData, destEndPoint);
        
        var session = _sessionManager.GetSession(packet.StaticHeader.AuthData, destEndPoint);
        session!.SetChallengeData(maskingIv, constructedWhoAreYouPacket.Item2.GetHeader());

        await connection.SendAsync(constructedWhoAreYouPacket.Item1, destEndPoint);
        _logger.LogInformation("Sent WHOAREYOU packet to {RemoteEndPoint}", destEndPoint);
    }
    
    private async Task SendResponseToOrdinaryPacketAsync(PacketProcessor packet, SessionMain sessionMain, IPEndPoint destEndPoint, IUdpConnection connection, byte[] response)
    {
        var maskingIv = RandomUtility.GenerateRandomData(PacketConstants.MaskingIvSize);
        var ordinaryPacket = _packetBuilder.BuildOrdinaryPacket(packet.StaticHeader.AuthData, maskingIv, sessionMain.MessageCount);
        var encryptedMessage = sessionMain.EncryptMessage(ordinaryPacket.Item2, maskingIv, response);
        var finalPacket = ByteArrayUtils.JoinByteArrays(ordinaryPacket.Item1, encryptedMessage);

        await connection.SendAsync(finalPacket, destEndPoint);
        _logger.LogInformation("Sent response to ORDINARY packet to {RemoteEndPoint}", destEndPoint);
    }
}