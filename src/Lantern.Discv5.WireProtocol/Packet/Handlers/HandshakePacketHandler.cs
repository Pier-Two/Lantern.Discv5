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
using NBitcoin.Secp256k1;

namespace Lantern.Discv5.WireProtocol.Packet.Handlers;

public class HandshakePacketHandler : PacketHandlerBase
{
    private readonly IIdentityManager _identityManager;
    private readonly ISessionManager _sessionManager;
    private readonly ITableManager _tableManager;
    private readonly IMessageResponder _messageResponder;
    private readonly IAesUtility _aesUtility;
    private readonly IPacketBuilder _packetBuilder;

    public HandshakePacketHandler(IIdentityManager identityManager, ISessionManager sessionManager, ITableManager tableManager, IMessageResponder messageResponder, IAesUtility aesUtility, IPacketBuilder packetBuilder)
    {
        _identityManager = identityManager;
        _sessionManager = sessionManager;
        _tableManager = tableManager;
        _messageResponder = messageResponder;
        _aesUtility = aesUtility;
        _packetBuilder = packetBuilder;
    }

    public override PacketType PacketType => PacketType.Handshake;

    public override async Task HandlePacket(IUdpConnection connection, UdpReceiveResult returnedResult)
    {
        Console.Write("\nReceived HANDSHAKE packet from " + returnedResult.RemoteEndPoint.Address + " => ");
        var packet = new PacketMain(_identityManager, _aesUtility, returnedResult.Buffer);
        var handshakePacket = HandshakePacketBase.CreateFromStaticHeader(packet.StaticHeader);
        var result = ObtainPublicKey(handshakePacket, handshakePacket.SrcId!, out var publicKey);

        if (!result)
        {
            Console.WriteLine("Cannot obtain public key from record. Unable to verify ID signature from HANDSHAKE packet.");
            return;
        }
        
        var session = _sessionManager.GetSession(handshakePacket.SrcId!, returnedResult.RemoteEndPoint);

        if (session == null)
        {
            Console.WriteLine("SessionMain not found. Cannot verify ID signature from HANDSHAKE packet.");
            return;
        }
        
        var idSignatureVerificationResult = session.VerifyIdSignature(handshakePacket, publicKey, _identityManager.NodeId); 

        if(idSignatureVerificationResult == false)
            throw new Exception("ID signature verification failed.");

        var decryptedMessage = session.DecryptMessageWithNewKeys(packet, handshakePacket, _identityManager.NodeId);

        if (decryptedMessage == null)
        {
            Console.WriteLine("Cannot decrypt message in the HANDSHAKE packet.");
            return;
        }
        
        Console.Write("Successfully decrypted HANDSHAKE packet => ");
        
        var replyPacket = PrepareMessageForHandshake(decryptedMessage, handshakePacket.SrcId!, session, returnedResult.RemoteEndPoint);
        
        if(replyPacket == null)
            return;
        
        await connection.SendAsync(replyPacket, returnedResult.RemoteEndPoint);
        Console.Write(" => Sent response to HANDSHAKE packet.\n");
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
            var nodeEntry = _tableManager.GetNodeEntry(senderNodeId);
            
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

        _tableManager.UpdateTable(senderRecord);
    
        senderPublicKey = senderRecord.GetEntry<EntrySecp256K1>("secp256k1").Value;
        return true;
    }
    
    private byte[]? PrepareMessageForHandshake(byte[] decryptedMessage, byte[] senderNodeId, Session.SessionMain sessionMain, IPEndPoint endPoint) 
    {
        var response = _messageResponder.HandleMessage(decryptedMessage, endPoint);

        if (response == null)
        {
            return null;
        }
        
        var maskingIv = RandomUtility.GenerateMaskingIv(PacketConstants.MaskingIvSize);
        var ordinaryPacket = _packetBuilder.BuildOrdinaryPacket(senderNodeId, maskingIv);
        var encryptedMessage = sessionMain.EncryptMessage(ordinaryPacket.Item2, maskingIv, response);
        
        return ByteArrayUtils.JoinByteArrays(ordinaryPacket.Item1, encryptedMessage);
    }
}