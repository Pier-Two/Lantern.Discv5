using System.Net.Sockets;
using Lantern.Discv5.Rlp;
using Lantern.Discv5.WireProtocol.Connection;
using Lantern.Discv5.WireProtocol.Identity;
using Lantern.Discv5.WireProtocol.Message;
using Lantern.Discv5.WireProtocol.Packet.Headers;
using Lantern.Discv5.WireProtocol.Packet.Types;
using Lantern.Discv5.WireProtocol.Session;
using Lantern.Discv5.WireProtocol.Table;

namespace Lantern.Discv5.WireProtocol.Packet.Handlers;

public class OrdinaryPacketHandler : PacketHandlerBase
{
    private readonly IIdentityManager _identityManager;
    private readonly ISessionManager _sessionManager;
    private readonly ITableManager _tableManager;
    private readonly IMessageHandler _messageHandler;

    public OrdinaryPacketHandler(IIdentityManager identityManager, ISessionManager sessionManager,
        ITableManager tableManager, IMessageHandler messageHandler)
    {
        _identityManager = identityManager;
        _sessionManager = sessionManager;
        _tableManager = tableManager;
        _messageHandler = messageHandler;
    }

    public override PacketType PacketType => PacketType.Ordinary;

    public override async Task HandlePacket(IUdpConnection connection, UdpReceiveResult returnedResult)
    {
        Console.Write("\nReceived ORDINARY packet => ");
        var selfRecord = _identityManager.Record;
        var selfNodeId = _identityManager.Verifier.GetNodeIdFromRecord(selfRecord);
        var packetBuffer = returnedResult.Buffer;
        var decryptedPacket = AESUtility.AesCtrDecrypt(selfNodeId[..16], packetBuffer[..16], packetBuffer[16..]);
        var staticHeader = StaticHeader.DecodeFromBytes(decryptedPacket);
        var sender = returnedResult.RemoteEndPoint;

        // Add the contents of the previous HandleOrdinaryPacket method here

        var destNodeId = staticHeader.AuthData;
        var destNodeRecord = _tableManager.GetNodeEntry(destNodeId).Record;

        if (destNodeRecord == null)
        {
            Console.WriteLine("Could not find destination record.");
            return;
        }

        var maskingIv = RandomUtility.GenerateMaskingIv(PacketConstants.MaskingIvSize);
        var constructedWhoAreYouPacket =
            await PacketConstructor.ConstructWhoAreYouPacket(destNodeId, staticHeader.Nonce, destNodeRecord!, maskingIv);
        var cryptoSession = _sessionManager.GetSession(destNodeId, sender);
        
        if (cryptoSession == null)
        {
            var challengeData = ByteArrayUtils.JoinByteArrays(maskingIv, constructedWhoAreYouPacket.Item2.GetHeader());
            _sessionManager.CreateSession(SessionType.Recipient, destNodeId, sender, challengeData);
            await connection.SendAsync(constructedWhoAreYouPacket.Item1, sender);
            Console.WriteLine("Sent WHOAREYOU packet.");
        }
        else
        {
            var sessionKeys = cryptoSession.CurrentSessionKeys;

            if (sessionKeys == null)
            {
                Console.WriteLine("Session keys are null. Cannot decrypt packet.");
            }
            else
            {
                try
                {
                    var encryptedMessage = packetBuffer[^staticHeader.EncryptedMessageLength..];
                    var maskedIv = packetBuffer[..16];
                    var messageAd = ByteArrayUtils.JoinByteArrays(maskedIv, staticHeader.GetHeader());
                    var decryptionKey = cryptoSession.SessionType == SessionType.Initiator ? sessionKeys.RecipientKey : sessionKeys.InitiatorKey;
                    var decryptedMessage = AESUtility.AesGcmDecrypt(decryptionKey, staticHeader.Nonce,
                        encryptedMessage, messageAd);
                    
                    Console.Write("Successfully decrypted ORDINARY packet" + " => ");
                    
                    var response = _messageHandler.HandleMessage(decryptedMessage);
                    cryptoSession.IsEstablished = true;
                    
                    if (response != null)
                    {
                        var newMaskingIv = RandomUtility.GenerateMaskingIv(PacketConstants.MaskingIvSize);
                        var ordinaryPacket = PacketConstructor.ConstructOrdinaryPacket(selfNodeId, destNodeId, newMaskingIv);
                        var newMessageAd = ByteArrayUtils.JoinByteArrays(newMaskingIv, ordinaryPacket.Result.Item2.GetHeader());
                        var encryptionKey = cryptoSession.SessionType == SessionType.Initiator ? sessionKeys.InitiatorKey : sessionKeys.RecipientKey;
                        var encryptedResponse = AESUtility.AesGcmEncrypt(encryptionKey, ordinaryPacket.Result.Item2.Nonce, response, newMessageAd);
                        var finalPacket = ByteArrayUtils.JoinByteArrays(ordinaryPacket.Result.Item1, encryptedResponse);
                        await connection.SendAsync(finalPacket, returnedResult.RemoteEndPoint);
                        Console.Write(" => Sent response to ORDINARY packet.\n");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }

            }
        }
    }
}