using System.Net.Sockets;
using Lantern.Discv5.Enr;
using Lantern.Discv5.Enr.EnrContent.Entries;
using Lantern.Discv5.Enr.EnrFactory;
using Lantern.Discv5.Enr.IdentityScheme.V4;
using Lantern.Discv5.Rlp;
using Lantern.Discv5.WireProtocol.Connection;
using Lantern.Discv5.WireProtocol.Identity;
using Lantern.Discv5.WireProtocol.Messages;
using Lantern.Discv5.WireProtocol.Packets.Headers;
using Lantern.Discv5.WireProtocol.Packets.Types;
using Lantern.Discv5.WireProtocol.Session;
using Lantern.Discv5.WireProtocol.Table;
using Lantern.Discv5.WireProtocol.Utility;
using NBitcoin.Secp256k1;

namespace Lantern.Discv5.WireProtocol.Packets.Handlers;

public class HandshakePacketHandler : PacketHandlerBase
{
    private readonly IIdentityManager _identityManager;
    private readonly ISessionManager _sessionManager;
    private readonly ITableManager _tableManager;
    private readonly IMessageHandler _messageHandler;

    public HandshakePacketHandler(IIdentityManager identityManager, ISessionManager sessionManager, ITableManager tableManager, IMessageHandler messageHandler)
    {
        _identityManager = identityManager;
        _sessionManager = sessionManager;
        _tableManager = tableManager;
        _messageHandler = messageHandler;
    }

    public override PacketType PacketType => PacketType.Handshake;

    public override async Task HandlePacket(IUdpConnection connection, UdpReceiveResult returnedResult)
    {
        Console.WriteLine("Received Handshake packet.");
        var selfRecord = _identityManager.Record;
        var selfNodeId = _identityManager.Verifier.GetNodeIdFromRecord(selfRecord);
        var rawPacket = returnedResult.Buffer;
        var decryptedPacket = AesUtils.AesCtrDecrypt(selfNodeId[..16], rawPacket[..16], rawPacket[16..]);
        var staticHeader = StaticHeader.DecodeFromBytes(decryptedPacket);
        var sender = returnedResult.RemoteEndPoint;

        // Add the contents of the previous HandleHandshakePacket method here
        var handshakePacket = HandshakePacket.DecodeAuthData(staticHeader.AuthData);
        var senderNodeId = handshakePacket.SrcId;
        var cryptoSession = _sessionManager.GetSession(senderNodeId!, sender);
        EnrRecord senderRecord;

        if (handshakePacket.Record.Length > 0)
        {
            var recordFactory = new EnrRecordFactory();
            senderRecord = recordFactory.CreateFromBytes(handshakePacket.Record);
        }
        else
        {
            senderRecord = _tableManager.GetEnrRecord(senderNodeId!);
        }
        _tableManager.AddEnrRecord(new NodeBucket(senderRecord, new IdentitySchemeV4Verifier()));

        var publicKey = senderRecord.GetEntry<EntrySecp256K1>("secp256k1").Value;
        var idSignatureVerificationResult = SessionUtils.VerifyIdSignature(handshakePacket.IdSignature, publicKey, cryptoSession.ChallengeData, handshakePacket.EphPubkey, selfNodeId, new Context());

        if(idSignatureVerificationResult == false)
            throw new Exception("Id signature verification failed.");

        var maskedIv = rawPacket[..16];
        var sharedSecret = cryptoSession.GenerateSharedSecretFromPrivateKey(handshakePacket.EphPubkey);
        var sessionKeys = SessionUtils.GenerateSessionKeys(sharedSecret,senderNodeId!, selfNodeId, cryptoSession.ChallengeData);
        var challengeData = ByteArrayUtils.JoinByteArrays(maskedIv, staticHeader.GetHeader());
        var encryptedMessage = rawPacket[^staticHeader.EncryptedMessageLength..];
        var decryptedMessage = AesUtils.AesGcmDecrypt(sessionKeys.InitiatorKey, staticHeader.Nonce, encryptedMessage, challengeData);
        Console.WriteLine("Successfully decrypted handshake packet.");
        _messageHandler.HandleMessage(decryptedMessage);
    }
}