using System.Net.Sockets;
using Lantern.Discv5.Rlp;
using Lantern.Discv5.WireProtocol.Connection;
using Lantern.Discv5.WireProtocol.Identity;
using Lantern.Discv5.WireProtocol.Messages;
using Lantern.Discv5.WireProtocol.Packets.Headers;
using Lantern.Discv5.WireProtocol.Packets.Types;
using Lantern.Discv5.WireProtocol.Session;
using Lantern.Discv5.WireProtocol.Table;
using Lantern.Discv5.WireProtocol.Utility;

namespace Lantern.Discv5.WireProtocol.Packets.Handlers;

public class OrdinaryPacketHandler : PacketHandlerBase
{
    private readonly IIdentityManager _identityManager;
    private readonly ISessionManager _sessionManager;
    private readonly ITableManager _tableManager;
    private readonly IMessageHandler _messageHandler;

    public OrdinaryPacketHandler(IIdentityManager identityManager, ISessionManager sessionManager, ITableManager tableManager, IMessageHandler messageHandler)
    {
        _identityManager = identityManager;
        _sessionManager = sessionManager;
        _tableManager = tableManager;
        _messageHandler = messageHandler;
    }

    public override PacketType PacketType => PacketType.Ordinary;

    public override async Task HandlePacket(IUdpConnection connection, UdpReceiveResult returnedResult)
    {
        Console.WriteLine("Received ordinary packet.");
        var selfRecord = _identityManager.Record;
        var selfNodeId = _identityManager.Verifier.GetNodeIdFromRecord(selfRecord);
        var packetBuffer = returnedResult.Buffer;
        var decryptedPacket = AesUtils.AesCtrDecrypt(selfNodeId[..16], packetBuffer[..16], packetBuffer[16..]);
        var staticHeader = StaticHeader.DecodeFromBytes(decryptedPacket);
        var sender = returnedResult.RemoteEndPoint;

        // Add the contents of the previous HandleOrdinaryPacket method here
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
                var encryptedMessage = packetBuffer[^staticHeader.EncryptedMessageLength..];
                var maskedIv = packetBuffer[..16];
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
}