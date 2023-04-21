using System.Net;
using System.Net.Sockets;
using System.Text;
using Lantern.Discv5.Enr;
using Lantern.Discv5.Enr.EnrContent;
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

namespace Lantern.Discv5.WireProtocol.Packets;

public class OldPacketService
{
    private readonly IdentityManager _identityManager;
    private readonly SessionManager _sessionManager;
    private readonly TableManager _tableManager;
    private readonly MessageHandler _messageHandler;
    private readonly TableOptions _tableOptions;

    public OldPacketService(IdentityManager identityManager, SessionManager sessionManager, TableOptions tableOptions)
    {
        _identityManager = identityManager;
        _sessionManager = sessionManager;
        _tableManager = new TableManager(tableOptions);
        _messageHandler = new MessageHandler(_tableManager);
        _tableOptions = tableOptions;
        LogIdentityManagerDetails(); // For debugging purposes only (ignore for now)
    }
    
    public async Task SendOrdinaryPacketForLookup(UdpConnection udpConnection)
    {
        var srcNodeId = _identityManager.Verifier.GetNodeIdFromRecord(_identityManager.Record);
        var destNode = _tableOptions.BootstrapEnrs[0];
        var destNodeId = _identityManager.Verifier.GetNodeIdFromRecord(destNode);
        var destEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 5500);
        var maskingIv = PacketUtils.GenerateMaskingIv();
        var packetNonce = PacketUtils.GenerateNonce();
        
        _sessionManager.SaveHandshakeInteraction(packetNonce, destNodeId);
        var constructedOrdinaryPacket = PacketConstructor.ConstructOrdinaryPacket(srcNodeId, destNodeId, packetNonce, maskingIv);
        await udpConnection.SendAsync(constructedOrdinaryPacket.Result.Item1, destEndPoint);
        Console.WriteLine("Sent ordinary packet for lookup.");
    }
    
    public async Task HandleIncomingPacket(UdpConnection connection, UdpReceiveResult returnedResult)
    {
        var selfRecord = _identityManager.Record;
        var selfNodeId = _identityManager.Verifier.GetNodeIdFromRecord(selfRecord);
        var packetBuffer = returnedResult.Buffer;
        var decryptedPacket = AesUtils.AesCtrDecrypt(selfNodeId[..16], packetBuffer[..16], packetBuffer[16..]);
        var staticHeader = StaticHeader.DecodeFromBytes(decryptedPacket);
        var sender = returnedResult.RemoteEndPoint;
        
        switch (staticHeader.Flag)
        {
            // Move handlers to Packet Handler class
            case (byte)PacketType.Ordinary: 
                Console.Write("\nReceived ordinary packet => ");
                await HandleOrdinaryPacket(connection, packetBuffer, staticHeader, sender);
                break;
            case (byte)PacketType.WhoAreYou:
                Console.Write("Received whoAreYou packet => ");
                var whoAreYouPacket = WhoAreYouPacket.DecodeAuthData(staticHeader.AuthData);
                await HandleWhoAreYouPacket(connection, staticHeader, packetBuffer, sender);
                break;
            case (byte)PacketType.Handshake:
                Console.Write("Received handshake packet.");
                var handshakePacket = HandshakePacket.DecodeAuthData(staticHeader.AuthData); 
                await HandleHandshakePacket(staticHeader, packetBuffer, handshakePacket, sender);
                break;
        }
    }
    
    private async Task HandleOrdinaryPacket(UdpConnection connection, byte[] rawPacket, StaticHeader staticHeader, IPEndPoint sender)
    {
        try
        {
            var destId = staticHeader.AuthData;
            var maskingIv = PacketUtils.GenerateMaskingIv();
            var constructedWhoAreYouPacket = await PacketConstructor.ConstructWhoAreYouPacket(destId, staticHeader.Nonce, _tableManager.GetEnrRecord(destId), maskingIv);
            var cryptoSession = _sessionManager.GetSession(destId, sender);
            
            if (cryptoSession == null)
            {
                var challengeData =
                    ByteArrayUtils.JoinByteArrays(maskingIv, constructedWhoAreYouPacket.Item2.GetHeader());
                _sessionManager.CreateSession(SessionType.Recipient, destId, sender, challengeData);
                await connection.SendAsync(constructedWhoAreYouPacket.Item1, sender);
                Console.WriteLine("Sent whoAreYou packet.");
            }
            else
            {
                var sessionKeys = cryptoSession.CurrentSessionKeys;
                var encryptedMessage = rawPacket[^staticHeader.EncryptedMessageLength..];
                var maskedIv = rawPacket[..16];
                var messageAd = ByteArrayUtils.JoinByteArrays(maskedIv, staticHeader.GetHeader());
                var decryptedMessage = AesUtils.AesGcmDecrypt(sessionKeys.RecipientKey, staticHeader.Nonce,
                    encryptedMessage, messageAd);
                Console.Write("Successfully decrypted ordinary packet. ");
                _messageHandler.HandleMessage(decryptedMessage);
            }
        }
        catch (Exception ex)
        {
            Console.Write(" => Failed to decrypt packet. Exception: " + ex);
        }

    }

    private async Task HandleWhoAreYouPacket(UdpConnection udpConnection, StaticHeader staticHeader, byte[] rawPacket, IPEndPoint sender)
    {
        var packetNonce = staticHeader.Nonce;
        var selfNodeRecord = _identityManager.Record;
        var selfNodeId = _identityManager.Verifier.GetNodeIdFromRecord(selfNodeRecord);
        var destNodeId = _sessionManager.GetHandshakeInteraction(packetNonce);
        
        if (destNodeId == null)
        {
            Console.WriteLine("Failed to get dest node id from packet nonce.");
            return;
        }
        
        var destNodeRecord = _tableManager.GetEnrRecord(destNodeId);
        var destNodePubkey = destNodeRecord.GetEntry<EntrySecp256K1>(EnrContentKey.Secp256K1).Value;
        var challengeData = ByteArrayUtils.JoinByteArrays(rawPacket.AsSpan()[..16], staticHeader.GetHeader());
        var cryptoSession = _sessionManager.GetSession(destNodeId, sender);

        if (cryptoSession == null)
        {
            cryptoSession = _sessionManager.CreateSession(SessionType.Initiator, destNodeId, sender, challengeData);
        }
        else
        {
            cryptoSession.ChallengeData = challengeData;
        }

        var ephemeralPubkey = cryptoSession.EphemeralPublicKey;
        var idSignature = cryptoSession.GenerateIdSignature(challengeData, ephemeralPubkey, destNodeId);
        var maskingIv = PacketUtils.GenerateMaskingIv();
        var sharedSecret = cryptoSession.GenerateSharedSecret(destNodePubkey); //cryptoSession.GenerateSessionKeys(destNodePubkey);
        var sessionKeys = SessionUtils.GenerateSessionKeys(sharedSecret, selfNodeId, destNodeId, challengeData);
        
        cryptoSession.CurrentSessionKeys = sessionKeys;

        var handshakePacket = await PacketConstructor.ConstructHandshakePacket(idSignature, ephemeralPubkey, selfNodeId, destNodeId, maskingIv, selfNodeRecord);
        var rawMessage = "This is a secret message.";
        var message = Encoding.UTF8.GetBytes(rawMessage);
        var messageAd = ByteArrayUtils.JoinByteArrays(maskingIv, handshakePacket.Item2.GetHeader());
        var encryptedMessage = AesUtils.AesGcmEncrypt(sessionKeys.InitiatorKey, handshakePacket.Item2.Nonce, message, messageAd);
        var finalPacket = ByteArrayUtils.JoinByteArrays(handshakePacket.Item1, encryptedMessage);
        await udpConnection.SendAsync(finalPacket, sender);
        Console.Write("Sent handshake packet with encrypted message: " + rawMessage + ".");
    }

    private async Task HandleHandshakePacket(StaticHeader staticHeader, byte[] rawPacket, HandshakePacket handshakePacket, IPEndPoint sender)
    {
        var senderNodeId = handshakePacket.SrcId;
        var selfNodeId = _identityManager.Verifier.GetNodeIdFromRecord(_identityManager.Record);
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
    
    private void LogIdentityManagerDetails()
    {
        Console.WriteLine("IDENTITY DETAILS");
        Console.WriteLine("================");
        Console.WriteLine("Ethereum Node Record: " + _identityManager.Record);
        Console.WriteLine("Ip address: " + _identityManager.Record.GetEntry<EntryIp>("ip").Value);
        Console.WriteLine("Listening on port: " + _identityManager.Record.GetEntry<EntryUdp>("udp").Value);
        Console.WriteLine("\nCOMMUNICATION LOGS");
        Console.WriteLine("=====================");
    }
}