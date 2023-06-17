using System.Net;
using System.Net.Sockets;
using Lantern.Discv5.Enr;
using Lantern.Discv5.Enr.EnrContent.Entries;
using Lantern.Discv5.Enr.EnrFactory;
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

public class HandshakePacketHandler : PacketHandlerBase
{
    private readonly IIdentityManager _identityManager;
    private readonly ISessionManager _sessionManager;
    private readonly IRoutingTable _routingTable;
    private readonly IMessageResponder _messageResponder;
    private readonly IAesUtility _aesUtility;
    private readonly IPacketBuilder _packetBuilder;
    private readonly ILogger<HandshakePacketHandler> _logger;

    public HandshakePacketHandler(IIdentityManager identityManager, ISessionManager sessionManager, IRoutingTable routingTable, IMessageResponder messageResponder, IAesUtility aesUtility, IPacketBuilder packetBuilder, ILoggerFactory loggerFactory)
    {
        _identityManager = identityManager;
        _sessionManager = sessionManager;
        _routingTable = routingTable;
        _messageResponder = messageResponder;
        _aesUtility = aesUtility;
        _packetBuilder = packetBuilder;
        _logger = loggerFactory.CreateLogger<HandshakePacketHandler>();
    }

    public override PacketType PacketType => PacketType.Handshake;

    public override async Task HandlePacket(IUdpConnection connection, UdpReceiveResult returnedResult)
    {
        _logger.LogInformation("Received HANDSHAKE packet from {RemoteEndPoint}", returnedResult.RemoteEndPoint);
        var packet = new PacketProcessor(_identityManager, _aesUtility, returnedResult.Buffer);
        var handshakePacket = HandshakePacketBase.CreateFromStaticHeader(packet.StaticHeader);
        var result = ObtainPublicKey(handshakePacket, handshakePacket.SrcId!, out var publicKey);

        if (!result)
        {
            _logger.LogWarning("Cannot obtain public key from record. Unable to verify ID signature from HANDSHAKE packet");
            return;
        }
        
        var session = _sessionManager.GetSession(handshakePacket.SrcId!, returnedResult.RemoteEndPoint);

        if (session == null)
        {
            _logger.LogWarning("Session not found. Cannot verify ID signature from HANDSHAKE packet");
            return;
        }
        
        var idSignatureVerificationResult = session.VerifyIdSignature(handshakePacket, publicKey, _identityManager.NodeId);

        if (idSignatureVerificationResult == false)
        {
            _logger.LogError("ID signature verification failed. Cannot decrypt message in the HANDSHAKE packet");
            return;
        }

        var decryptedMessage = session.DecryptMessageWithNewKeys(packet, handshakePacket, _identityManager.NodeId);

        if (decryptedMessage == null)
        {
            _logger.LogWarning("Cannot decrypt message in the HANDSHAKE packet");
            return;
        }
        
        _logger.LogDebug("Successfully decrypted HANDSHAKE packet");
        
        var replyPacket = await PrepareMessageForHandshake(decryptedMessage, handshakePacket.SrcId!, session, returnedResult.RemoteEndPoint);
        
        if(replyPacket == null)
            return;
        
        await connection.SendAsync(replyPacket, returnedResult.RemoteEndPoint);
        _logger.LogDebug("Sent response to HANDSHAKE packet");
    }

    private bool ObtainPublicKey(HandshakePacketBase handshakePacketBase, byte[]? senderNodeId, out byte[] senderPublicKey)
    {
        EnrRecord? senderRecord = null;
    
        if (handshakePacketBase.Record?.Length > 0)
        {
            var recordFactory = new EnrRecordFactory();
            senderRecord = recordFactory.CreateFromBytes(handshakePacketBase.Record);
        }
        else if (senderNodeId != null)
        {
            var nodeEntry = _routingTable.GetNodeEntry(senderNodeId);
            
            if (nodeEntry != null)
            {
                senderRecord = nodeEntry.Record;
                senderPublicKey = senderRecord.GetEntry<EntrySecp256K1>("secp256k1").Value;
                return true;
            }
        }

        if (senderRecord == null)
        {
            senderPublicKey = Array.Empty<byte>();
            return false;
        }

        _routingTable.UpdateFromEnr(senderRecord);
    
        senderPublicKey = senderRecord.GetEntry<EntrySecp256K1>("secp256k1").Value;
        return true;
    }
    
    private async Task <byte[]?> PrepareMessageForHandshake(byte[] decryptedMessage, byte[] senderNodeId, SessionMain sessionMain, IPEndPoint endPoint) 
    {
        var response = await _messageResponder.HandleMessage(decryptedMessage, endPoint);

        if (response == null)
        {
            return null;
        }
        
        var maskingIv = RandomUtility.GenerateRandomData(PacketConstants.MaskingIvSize);
        var ordinaryPacket = _packetBuilder.BuildOrdinaryPacket(senderNodeId, maskingIv, sessionMain.MessageCount);
        var encryptedMessage = sessionMain.EncryptMessage(ordinaryPacket.Item2, maskingIv, response);
        
        return ByteArrayUtils.JoinByteArrays(ordinaryPacket.Item1, encryptedMessage);
    }
}