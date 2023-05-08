using System.Net.Sockets;
using Lantern.Discv5.Enr;
using Lantern.Discv5.Enr.EnrContent.Entries;
using Lantern.Discv5.Enr.EnrFactory;
using Lantern.Discv5.Enr.IdentityScheme.V4;
using Lantern.Discv5.Rlp;
using Lantern.Discv5.WireProtocol.Connection;
using Lantern.Discv5.WireProtocol.Identity;
using Lantern.Discv5.WireProtocol.Message;
using Lantern.Discv5.WireProtocol.Packet.Headers;
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

    public HandshakePacketHandler(IIdentityManager identityManager, ISessionManager sessionManager, ITableManager tableManager, IMessageResponder messageResponder)
    {
        _identityManager = identityManager;
        _sessionManager = sessionManager;
        _tableManager = tableManager;
        _messageResponder = messageResponder;
    }

    public override PacketType PacketType => PacketType.Handshake;

    public override async Task HandlePacket(IUdpConnection connection, UdpReceiveResult returnedResult)
    {
        Console.Write("\nReceived HANDSHAKE packet from " + returnedResult.RemoteEndPoint.Address + " => ");
        var selfRecord = _identityManager.Record;
        var selfNodeId = _identityManager.Verifier.GetNodeIdFromRecord(selfRecord);
        var rawPacket = returnedResult.Buffer;
        var decryptedPacket = AESUtility.AesCtrDecrypt(selfNodeId[..16], rawPacket[..16], rawPacket[16..]);
        var staticHeader = StaticHeader.DecodeFromBytes(decryptedPacket);
        var sender = returnedResult.RemoteEndPoint;
        var handshakePacket = HandshakePacket.DecodeAuthData(staticHeader.AuthData);
        var senderNodeId = handshakePacket.SrcId;
        var cryptoSession = _sessionManager.GetSession(senderNodeId!, sender);
        var result = ObtainPublicKeyFromRecord(handshakePacket, senderNodeId, out var publicKey);

        if (!result)
        {
            Console.WriteLine("Cannot obtain public key from record. Cannot verify Id signature.");
            return;
        }
        
        var idSignatureVerificationResult = SessionUtility.VerifyIdSignature(handshakePacket.IdSignature, publicKey, cryptoSession.ChallengeData, handshakePacket.EphPubkey, selfNodeId, new Context());

        if(idSignatureVerificationResult == false)
            throw new Exception("Id signature verification failed.");

        var maskedIv = rawPacket[..16];
        var sharedSecret = cryptoSession.GenerateSharedSecretFromPrivateKey(handshakePacket.EphPubkey);
        var sessionKeys = SessionUtility.GenerateSessionKeys(sharedSecret,senderNodeId!, selfNodeId, cryptoSession.ChallengeData);
        var challengeData = ByteArrayUtils.JoinByteArrays(maskedIv, staticHeader.GetHeader());
        var encryptedMessage = rawPacket[^staticHeader.EncryptedMessageLength..];
        var decryptedMessage = AESUtility.AesGcmDecrypt(sessionKeys.InitiatorKey, staticHeader.Nonce, encryptedMessage, challengeData);
        
        cryptoSession.CurrentSessionKeys = sessionKeys;
        Console.Write("Successfully decrypted HANDSHAKE packet => ");
        
        var replyPacket = PrepareMessageForHandshake(decryptedMessage, selfNodeId, senderNodeId, cryptoSession);
        
        if(replyPacket == null)
            return;
        
        await connection.SendAsync(replyPacket, returnedResult.RemoteEndPoint);
        Console.Write(" => Sent response to HANDSHAKE packet.\n");
    }

    private bool ObtainPublicKeyFromRecord(HandshakePacket handshakePacket, byte[]? senderNodeId, out byte[] senderPublicKey)
    {
        EnrRecord? senderRecord;
        
        if (handshakePacket.Record is { Length: > 0 })
        {
            var recordFactory = new EnrRecordFactory();
            senderRecord = recordFactory.CreateFromBytes(handshakePacket.Record);
        }
        else
        {
            if (senderNodeId != null)
            {
                senderRecord = _tableManager.GetNodeEntry(senderNodeId).Record;
            }
            else
            {
                senderPublicKey = Array.Empty<byte>();
                return false;
            }
        }

        _tableManager.UpdateTable(senderRecord);
        
        senderPublicKey = senderRecord.GetEntry<EntrySecp256K1>("secp256k1").Value;
        return true;
    }
    
    private byte[]? PrepareMessageForHandshake(byte[] decryptedMessage, byte[] selfNodeId, byte[] senderNodeId, CryptoSession session) 
    {
        var response = _messageResponder.HandleMessage(decryptedMessage);

        if (response == null)
        {
            return null;
        }
        
        var newMaskingIv = RandomUtility.GenerateMaskingIv(PacketConstants.MaskingIvSize);
        var ordinaryPacket = PacketConstructor.ConstructOrdinaryPacket(selfNodeId, senderNodeId, newMaskingIv);
        var encryptedMes = session.EncryptMessage(ordinaryPacket.Result.Item2, response, newMaskingIv);
        return ByteArrayUtils.JoinByteArrays(ordinaryPacket.Result.Item1, encryptedMes);
    }
}