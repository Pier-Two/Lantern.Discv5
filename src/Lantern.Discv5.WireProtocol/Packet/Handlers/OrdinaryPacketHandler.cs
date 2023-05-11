using System.Net;
using System.Net.Sockets;
using Lantern.Discv5.Enr;
using Lantern.Discv5.Rlp;
using Lantern.Discv5.WireProtocol.Connection;
using Lantern.Discv5.WireProtocol.Identity;
using Lantern.Discv5.WireProtocol.Message;
using Lantern.Discv5.WireProtocol.Packet.Headers;
using Lantern.Discv5.WireProtocol.Packet.Types;
using Lantern.Discv5.WireProtocol.Session;
using Lantern.Discv5.WireProtocol.Table;
using Lantern.Discv5.WireProtocol.Utility;

namespace Lantern.Discv5.WireProtocol.Packet.Handlers;

public class OrdinaryPacketHandler : PacketHandlerBase
{
    private readonly IIdentityManager _identityManager;
    private readonly ISessionManager _sessionManager;
    private readonly ITableManager _tableManager;
    private readonly IMessageResponder _messageResponder;
    private readonly IAesUtility _aesUtility;
    private readonly IPacketBuilder _packetBuilder;

    public OrdinaryPacketHandler(IIdentityManager identityManager, ISessionManager sessionManager,
        ITableManager tableManager, IMessageResponder messageResponder, IAesUtility aesUtility, IPacketBuilder packetBuilder)
    {
        _identityManager = identityManager;
        _sessionManager = sessionManager;
        _tableManager = tableManager;
        _messageResponder = messageResponder;
        _aesUtility = aesUtility;
        _packetBuilder = packetBuilder;
    }

    public override PacketType PacketType => PacketType.Ordinary;

    public override async Task HandlePacket(IUdpConnection connection, UdpReceiveResult returnedResult)
    {
        Console.Write("\nReceived ORDINARY packet from " + returnedResult.RemoteEndPoint.Address + " => ");
        var packet = new PacketProcessor(_identityManager, _aesUtility, returnedResult.Buffer);
        var nodeEntry = _tableManager.GetNodeEntry(packet.StaticHeader.AuthData);
        
        if(nodeEntry == null)
        {
            await SendWhoAreYouPacketWithoutEnrAsync(packet, returnedResult.RemoteEndPoint, connection);
            return;
        }
        
        var session = _sessionManager.GetSession(packet.StaticHeader.AuthData, returnedResult.RemoteEndPoint);
        
        if (session == null)
        {
            await SendWhoAreYouPacketAsync(packet, nodeEntry.Record, returnedResult.RemoteEndPoint, connection);
            return;
        }

        var decryptedMessage = session.DecryptMessage(packet);

        if (decryptedMessage == null)
        {
            await SendWhoAreYouPacketAsync(packet, nodeEntry.Record, returnedResult.RemoteEndPoint, connection);
            return;
        }
        
        Console.Write("Successfully decrypted ORDINARY packet" + " => ");
                    
        var response = _messageResponder.HandleMessage(decryptedMessage, returnedResult.RemoteEndPoint);

        if (response != null)
        {
            await SendResponseToOrdinaryPacketAsync(packet, session, returnedResult.RemoteEndPoint, connection,
                response);
        }
    }

    private async Task SendWhoAreYouPacketWithoutEnrAsync(PacketProcessor packet, IPEndPoint destEndPoint, IUdpConnection connection)
    {
        Console.Write("Could not find record in the table for node: " + Convert.ToHexString(packet.StaticHeader.AuthData));
        
        var maskingIv = RandomUtility.GenerateMaskingIv(PacketConstants.MaskingIvSize);
        var whoAreYouPacket = _packetBuilder.BuildWhoAreYouPacketWithoutEnr(packet.StaticHeader.AuthData, packet.StaticHeader.Nonce, maskingIv);

        _sessionManager.CreateSession(SessionType.Recipient, packet.StaticHeader.AuthData, destEndPoint);
        
        var session = _sessionManager.GetSession(packet.StaticHeader.AuthData, destEndPoint);
        session!.SetChallengeData(maskingIv, whoAreYouPacket.Item2.GetHeader()); 
        
        await connection.SendAsync(whoAreYouPacket.Item1, destEndPoint);
        Console.Write(" => Sent WHOAREYOU packet.\n");
    }

    private async Task SendWhoAreYouPacketAsync(PacketProcessor packet, EnrRecord destNodeRecord, IPEndPoint destEndPoint, IUdpConnection connection)
    {
        Console.WriteLine("Cannot decrypt ORDINARY packet. No sessionMain found.");
        
        var maskingIv = RandomUtility.GenerateMaskingIv(PacketConstants.MaskingIvSize);
        var constructedWhoAreYouPacket = _packetBuilder.BuildWhoAreYouPacket(packet.StaticHeader.AuthData, packet.StaticHeader.Nonce, destNodeRecord!, maskingIv);
       
        _sessionManager.CreateSession(SessionType.Recipient, packet.StaticHeader.AuthData, destEndPoint);
        
        var session = _sessionManager.GetSession(packet.StaticHeader.AuthData, destEndPoint);
        session!.SetChallengeData(maskingIv, constructedWhoAreYouPacket.Item2.GetHeader());

        await connection.SendAsync(constructedWhoAreYouPacket.Item1, destEndPoint);
        Console.WriteLine("Sent WHOAREYOU packet.");
    }
    
    private async Task SendResponseToOrdinaryPacketAsync(PacketProcessor packet, SessionMain sessionMain, IPEndPoint destEndPoint, IUdpConnection connection, byte[] response)
    {
        var maskingIv = RandomUtility.GenerateMaskingIv(PacketConstants.MaskingIvSize);
        var ordinaryPacket = _packetBuilder.BuildOrdinaryPacket(packet.StaticHeader.AuthData, maskingIv, sessionMain.MessageCount);
        var encryptedMessage = sessionMain.EncryptMessage(ordinaryPacket.Item2, maskingIv, response);
        var finalPacket = ByteArrayUtils.JoinByteArrays(ordinaryPacket.Item1, encryptedMessage);

        await connection.SendAsync(finalPacket, destEndPoint);
        Console.Write(" => Sent response to ORDINARY packet.\n");
    }
}