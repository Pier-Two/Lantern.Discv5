using System.Net;
using System.Net.Sockets;
using Lantern.Discv5.Enr.EnrContent;
using Lantern.Discv5.Enr.EnrContent.Entries;
using Lantern.Discv5.Rlp;
using Lantern.Discv5.WireProtocol.Connection;
using Lantern.Discv5.WireProtocol.Identity;
using Lantern.Discv5.WireProtocol.Message;
using Lantern.Discv5.WireProtocol.Packet.Types;
using Lantern.Discv5.WireProtocol.Session;
using Lantern.Discv5.WireProtocol.Table;
using Lantern.Discv5.WireProtocol.Utility;

namespace Lantern.Discv5.WireProtocol.Packet.Handlers;

public class WhoAreYouPacketHandler : PacketHandlerBase
{
    private readonly IIdentityManager _identityManager;
    private readonly ISessionManager _sessionManager;
    private readonly ITableManager _tableManager;
    private readonly IMessageRequester _messageRequester;
    private readonly IAesUtility _aesUtility;
    private readonly IPacketBuilder _packetBuilder;
    
    public WhoAreYouPacketHandler(IIdentityManager identityManager, ISessionManager sessionManager, ITableManager tableManager, IMessageRequester messageRequester, IAesUtility aesUtility, IPacketBuilder packetBuilder)
    {
        _identityManager = identityManager;
        _sessionManager = sessionManager;
        _tableManager = tableManager;
        _messageRequester = messageRequester;
        _aesUtility = aesUtility;
        _packetBuilder = packetBuilder;
    }

    public override PacketType PacketType => PacketType.WhoAreYou;

    public override async Task HandlePacket(IUdpConnection connection, UdpReceiveResult returnedResult)
    {
        Console.Write("\nReceived WHOAREYOU packet from " + returnedResult.RemoteEndPoint.Address + " => ");
        var packet = new PacketProcessor(_identityManager, _aesUtility, returnedResult.Buffer);
        var destNodeId = _sessionManager.GetHandshakeInteraction(packet.StaticHeader.Nonce);
        
        if (destNodeId == null)
        {
            Console.WriteLine("Failed to get dest node id from packet nonce.");
            return;
        }
        
        var nodeEntry = _tableManager.GetNodeEntry(destNodeId);
        
        if(nodeEntry == null)
        {
            Console.WriteLine("Failed to get node entry from the ENR table at node id: " + Convert.ToHexString(destNodeId));
            return;
        }
        
        var session = GenerateOrUpdateSession(packet, destNodeId, returnedResult.RemoteEndPoint);
        var message = _messageRequester.ConstructMessage(MessageType.Ping, destNodeId);
        
        if(message == null)
        {
            Console.WriteLine("Failed to construct PING message.");
            return;
        }
        
        var idSignatureNew = session.GenerateIdSignature(destNodeId);
        var maskingIv = RandomUtility.GenerateMaskingIv(PacketConstants.MaskingIvSize);
        var handshakePacket = _packetBuilder.BuildHandshakePacket(idSignatureNew, session.EphemeralPublicKey, destNodeId, maskingIv, session.MessageCount);
        var encryptedMessage = session.EncryptMessageWithNewKeys(nodeEntry.Record, handshakePacket.Item2, _identityManager.NodeId, message, maskingIv);
        var finalPacket = ByteArrayUtils.JoinByteArrays(handshakePacket.Item1, encryptedMessage);
        
        await connection.SendAsync(finalPacket, returnedResult.RemoteEndPoint);
        Console.Write(" => Sent HANDSHAKE packet with encrypted message. " + "\n");
    }

    private SessionMain GenerateOrUpdateSession(PacketProcessor packet, byte[] destNodeId, IPEndPoint destEndPoint)
    {
        var session = _sessionManager.GetSession(destNodeId, destEndPoint);

        if (session == null)
        {
            Console.Write("Creating new session with node: " + destEndPoint);
            session = _sessionManager.CreateSession(SessionType.Initiator, destNodeId, destEndPoint);
        }
        
        session.SetChallengeData(packet.MaskingIv, packet.StaticHeader.GetHeader());

        return session;
    }
}