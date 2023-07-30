using System.Net;
using System.Net.Sockets;
using Lantern.Discv5.Enr;
using Lantern.Discv5.Enr.EnrContent;
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
    private readonly IUdpConnection _connection;
    private readonly IPacketBuilder _packetBuilder;
    private readonly IPacketProcessor _packetProcessor;
    private readonly ILogger<HandshakePacketHandler> _logger;

    public HandshakePacketHandler(IIdentityManager identityManager, ISessionManager sessionManager, 
        IRoutingTable routingTable, IMessageResponder messageResponder, IUdpConnection udpConnection, 
        IPacketBuilder packetBuilder, IPacketProcessor packetProcessor, ILoggerFactory loggerFactory)
    {
        _identityManager = identityManager;
        _sessionManager = sessionManager;
        _routingTable = routingTable;
        _messageResponder = messageResponder;
        _connection = udpConnection;
        _packetBuilder = packetBuilder;
        _packetProcessor = packetProcessor;
        _logger = loggerFactory.CreateLogger<HandshakePacketHandler>();
    }

    public override PacketType PacketType => PacketType.Handshake;

    public override async Task HandlePacket(UdpReceiveResult returnedResult)
    {
        _logger.LogInformation("Received HANDSHAKE packet from {RemoteEndPoint}", returnedResult.RemoteEndPoint);
        var packet = returnedResult.Buffer;
        var handshakePacket = HandshakePacketBase.CreateFromStaticHeader(_packetProcessor.GetStaticHeader(packet));
        var publicKey = ObtainPublicKey(handshakePacket, handshakePacket.SrcId!);
        
        if (publicKey == null)
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

        if (!idSignatureVerificationResult)
        {
            _logger.LogError("ID signature verification failed. Cannot decrypt message in the HANDSHAKE packet");
            return;
        }

        var decryptedMessage = session.DecryptMessageWithNewKeys(_packetProcessor.GetStaticHeader(packet), _packetProcessor.GetMaskingIv(packet), _packetProcessor.GetEncryptedMessage(packet),handshakePacket, _identityManager.NodeId);

        if (decryptedMessage == null)
        {
            _logger.LogWarning("Cannot decrypt message in the HANDSHAKE packet");
            return;
        }
        
        _logger.LogDebug("Successfully decrypted HANDSHAKE packet");
        
        var replies = await PrepareMessageForHandshake(decryptedMessage, handshakePacket.SrcId!, session, returnedResult.RemoteEndPoint);
        
        if(replies == null || replies.Length == 0)
            return;

        foreach (var reply in replies)
        {
            await _connection.SendAsync(reply, returnedResult.RemoteEndPoint);
            _logger.LogInformation("Sent response to HANDSHAKE packet");
        }
    }

    private byte[]? ObtainPublicKey(HandshakePacketBase handshakePacketBase, byte[]? senderNodeId)
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
                return senderRecord.GetEntry<EntrySecp256K1>(EnrContentKey.Secp256K1).Value;
            }
        }

        if (senderRecord == null)
        {
            return null;
        }

        _routingTable.UpdateFromEnr(senderRecord);
        
        return senderRecord.GetEntry<EntrySecp256K1>(EnrContentKey.Secp256K1).Value;
    }
    
    private async Task <byte[][]?> PrepareMessageForHandshake(byte[] decryptedMessage, byte[] senderNodeId, ISessionMain sessionMain, IPEndPoint endPoint) 
    {
        var responses = await _messageResponder.HandleMessageAsync(decryptedMessage, endPoint);

        if (responses == null || responses.Length == 0)
        {
            return null;
        }

        var responsesList = new List<byte[]>();

        foreach (var response in responses)
        {
            var maskingIv = RandomUtility.GenerateRandomData(PacketConstants.MaskingIvSize);
            var ordinaryPacket = _packetBuilder.BuildOrdinaryPacket(response, senderNodeId, maskingIv, sessionMain.MessageCount);
            var encryptedMessage = sessionMain.EncryptMessage(ordinaryPacket.Item2, maskingIv, response);
            responsesList.Add(ByteArrayUtils.JoinByteArrays(ordinaryPacket.Item1, encryptedMessage));
        }

        return responsesList.ToArray();
    }
}