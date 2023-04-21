using System.Net.Sockets;
using System.Text;
using Lantern.Discv5.Enr.EnrContent;
using Lantern.Discv5.Enr.EnrContent.Entries;
using Lantern.Discv5.Rlp;
using Lantern.Discv5.WireProtocol.Connection;
using Lantern.Discv5.WireProtocol.Identity;
using Lantern.Discv5.WireProtocol.Packets.Headers;
using Lantern.Discv5.WireProtocol.Packets.Types;
using Lantern.Discv5.WireProtocol.Session;
using Lantern.Discv5.WireProtocol.Table;
using Lantern.Discv5.WireProtocol.Utility;

namespace Lantern.Discv5.WireProtocol.Packets.Handlers;

public class WhoAreYouPacketHandler : PacketHandlerBase
{
    private readonly IIdentityManager _identityManager;
    private readonly ISessionManager _sessionManager;
    private readonly ITableManager _tableManager;

    public WhoAreYouPacketHandler(IIdentityManager identityManager, ISessionManager sessionManager, ITableManager tableManager)
    {
        _identityManager = identityManager;
        _sessionManager = sessionManager;
        _tableManager = tableManager;
    }

    public override PacketType PacketType => PacketType.WhoAreYou;

    public override async Task HandlePacket(IUdpConnection udpConnection, UdpReceiveResult returnedResult)
    {
        Console.WriteLine("Received whoAreYou packet.");
        var selfRecord = _identityManager.Record;
        var selfNodeId = _identityManager.Verifier.GetNodeIdFromRecord(selfRecord);
        var packetBuffer = returnedResult.Buffer;
        var decryptedPacket = AesUtils.AesCtrDecrypt(selfNodeId[..16], packetBuffer[..16], packetBuffer[16..]);
        var staticHeader = StaticHeader.DecodeFromBytes(decryptedPacket);
        var packetNonce = staticHeader.Nonce;
        var selfNodeRecord = _identityManager.Record;
        var destNodeId = _sessionManager.GetHandshakeInteraction(packetNonce);
        
        if (destNodeId == null)
        {
            Console.WriteLine("Failed to get dest node id from packet nonce.");
            return;
        }
        
        var destNodeRecord = _tableManager.GetEnrRecord(destNodeId);
        var destNodePubkey = destNodeRecord.GetEntry<EntrySecp256K1>(EnrContentKey.Secp256K1).Value;
        var challengeData = ByteArrayUtils.JoinByteArrays(returnedResult.Buffer.AsSpan()[..16], staticHeader.GetHeader());
        var cryptoSession = _sessionManager.GetSession(destNodeId, returnedResult.RemoteEndPoint);

        if (cryptoSession == null)
        {
            cryptoSession = _sessionManager.CreateSession(SessionType.Initiator, destNodeId, returnedResult.RemoteEndPoint, challengeData);
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
        await udpConnection.SendAsync(finalPacket, returnedResult.RemoteEndPoint);
        Console.Write("Sent handshake packet with encrypted message: " + rawMessage + ".");
    }
}