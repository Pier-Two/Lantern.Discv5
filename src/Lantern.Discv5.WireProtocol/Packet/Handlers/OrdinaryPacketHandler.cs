using System.Net;
using System.Net.Sockets;
using Lantern.Discv5.Enr;
using Lantern.Discv5.Rlp;
using Lantern.Discv5.WireProtocol.Connection;
using Lantern.Discv5.WireProtocol.Message;
using Lantern.Discv5.WireProtocol.Packet.Headers;
using Lantern.Discv5.WireProtocol.Packet.Types;
using Lantern.Discv5.WireProtocol.Session;
using Lantern.Discv5.WireProtocol.Table;
using Lantern.Discv5.WireProtocol.Utility;
using Microsoft.Extensions.Logging;

namespace Lantern.Discv5.WireProtocol.Packet.Handlers;

public class OrdinaryPacketHandler : PacketHandlerBase
{
    private readonly ISessionManager _sessionManager;
    private readonly IRoutingTable _routingTable;
    private readonly IMessageResponder _messageResponder;
    private readonly IUdpConnection _connection;
    private readonly IPacketBuilder _packetBuilder;
    private readonly IPacketProcessor _packetProcessor;
    private readonly ILogger<OrdinaryPacketHandler> _logger;

    public OrdinaryPacketHandler(ISessionManager sessionManager, IRoutingTable routingTable, 
        IMessageResponder messageResponder, IUdpConnection udpConnection, IPacketBuilder packetBuilder, 
        IPacketProcessor packetProcessor, ILoggerFactory loggerFactory)
    {
        _sessionManager = sessionManager;
        _routingTable = routingTable;
        _messageResponder = messageResponder;
        _connection = udpConnection;
        _packetBuilder = packetBuilder;
        _packetProcessor = packetProcessor;
        _logger = loggerFactory.CreateLogger<OrdinaryPacketHandler>();
    }

    public override PacketType PacketType => PacketType.Ordinary;

    public override async Task HandlePacket(UdpReceiveResult returnedResult)
    {
        _logger.LogInformation("Received ORDINARY packet from {Address}", returnedResult.RemoteEndPoint.Address);
        
        var staticHeader = _packetProcessor.GetStaticHeader(returnedResult.Buffer);
        var maskingIv = _packetProcessor.GetMaskingIv(returnedResult.Buffer);
        var encryptedMessage = _packetProcessor.GetEncryptedMessage(returnedResult.Buffer);
        var nodeEntry = _routingTable.GetNodeEntry(staticHeader.AuthData);

        if(nodeEntry == null)
        {
            _logger.LogWarning("Could not find record in the table for node: {NodeId}", Convert.ToHexString(staticHeader.AuthData));
            await SendWhoAreYouPacketWithoutEnrAsync(staticHeader, returnedResult.RemoteEndPoint, _connection);
            return;
        }
        
        var session = _sessionManager.GetSession(staticHeader.AuthData, returnedResult.RemoteEndPoint);
        
        if (session == null)
        {
            _logger.LogWarning("Cannot decrypt ORDINARY packet. No session found, sending WHOAREYOU packet");
            await SendWhoAreYouPacketAsync(staticHeader, nodeEntry.Record, returnedResult.RemoteEndPoint, _connection);
            return;
        }

        var decryptedMessage = session.DecryptMessage(staticHeader, maskingIv, encryptedMessage);

        if (decryptedMessage == null)
        {
            Console.WriteLine("Decrypted message is null");
            _logger.LogWarning("Cannot decrypt ORDINARY packet. Decryption failed, sending WHOAREYOU packet");
            await SendWhoAreYouPacketAsync(staticHeader, nodeEntry.Record, returnedResult.RemoteEndPoint, _connection);
            return;
        }
        
        _logger.LogDebug("Successfully decrypted ORDINARY packet");

        var response = await _messageResponder.HandleMessageAsync(decryptedMessage, returnedResult.RemoteEndPoint);

        if (response != null)
        {
            await SendResponseToOrdinaryPacketAsync(staticHeader, session, returnedResult.RemoteEndPoint, _connection,
                response);
        }
    }

    private async Task SendWhoAreYouPacketWithoutEnrAsync(StaticHeader staticHeader, IPEndPoint destEndPoint, IUdpConnection connection)
    {
        var maskingIv = RandomUtility.GenerateRandomData(PacketConstants.MaskingIvSize);
        var whoAreYouPacket = _packetBuilder.BuildWhoAreYouPacketWithoutEnr(staticHeader.AuthData, staticHeader.Nonce, maskingIv);
        var session = _sessionManager.CreateSession(SessionType.Recipient, staticHeader.AuthData, destEndPoint);

        session.SetChallengeData(maskingIv, whoAreYouPacket.Item2.GetHeader()); 
        
        await connection.SendAsync(whoAreYouPacket.Item1, destEndPoint);
        _logger.LogInformation("Sent WHOAREYOU packet to {RemoteEndPoint}", destEndPoint);
    }

    private async Task SendWhoAreYouPacketAsync(StaticHeader staticHeader, EnrRecord destNodeRecord, IPEndPoint destEndPoint, IUdpConnection connection)
    {
        var maskingIv = RandomUtility.GenerateRandomData(PacketConstants.MaskingIvSize);
        var constructedWhoAreYouPacket = _packetBuilder.BuildWhoAreYouPacket(staticHeader.AuthData, staticHeader.Nonce, destNodeRecord, maskingIv);
        var session = _sessionManager.CreateSession(SessionType.Recipient, staticHeader.AuthData, destEndPoint);

        session.SetChallengeData(maskingIv, constructedWhoAreYouPacket.Item2.GetHeader());

        await connection.SendAsync(constructedWhoAreYouPacket.Item1, destEndPoint);
        _logger.LogInformation("Sent WHOAREYOU packet to {RemoteEndPoint}", destEndPoint);
    }
    
    private async Task SendResponseToOrdinaryPacketAsync(StaticHeader staticHeader, ISessionMain sessionMain, IPEndPoint destEndPoint, IUdpConnection connection, byte[] response)
    {
        var maskingIv = RandomUtility.GenerateRandomData(PacketConstants.MaskingIvSize);
        var ordinaryPacket = _packetBuilder.BuildOrdinaryPacket(response,staticHeader.AuthData, maskingIv, sessionMain.MessageCount);
        var encryptedMessage = sessionMain.EncryptMessage(ordinaryPacket.Item2, maskingIv, response);
        var finalPacket = ByteArrayUtils.JoinByteArrays(ordinaryPacket.Item1, encryptedMessage);

        await connection.SendAsync(finalPacket, destEndPoint);
        _logger.LogInformation("Sent response to ORDINARY packet to {RemoteEndPoint}", destEndPoint);
    }
}