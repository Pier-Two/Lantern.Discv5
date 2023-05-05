using System.Net.Sockets;
using Lantern.Discv5.Enr.EnrContent;
using Lantern.Discv5.Enr.EnrContent.Entries;
using Lantern.Discv5.Rlp;
using Lantern.Discv5.WireProtocol.Connection;
using Lantern.Discv5.WireProtocol.Identity;
using Lantern.Discv5.WireProtocol.Message;
using Lantern.Discv5.WireProtocol.Packet.Headers;
using Lantern.Discv5.WireProtocol.Packet.Types;
using Lantern.Discv5.WireProtocol.Session;
using Lantern.Discv5.WireProtocol.Table;

namespace Lantern.Discv5.WireProtocol.Packet.Handlers;

public class WhoAreYouPacketHandler : PacketHandlerBase
{
    private readonly IIdentityManager _identityManager;
    private readonly ISessionManager _sessionManager;
    private readonly ITableManager _tableManager;
    private readonly IMessageConstructor _messageConstructor;
    
    public WhoAreYouPacketHandler(IIdentityManager identityManager, ISessionManager sessionManager, ITableManager tableManager, IMessageConstructor messageConstructor)
    {
        _identityManager = identityManager;
        _sessionManager = sessionManager;
        _tableManager = tableManager;
        _messageConstructor = messageConstructor;
    }

    public override PacketType PacketType => PacketType.WhoAreYou;

    public override async Task HandlePacket(IUdpConnection connection, UdpReceiveResult returnedResult)
    {
        Console.Write("\nReceived WHOAREYOU packet => ");
        var selfRecord = _identityManager.Record;
        var selfNodeId = _identityManager.Verifier.GetNodeIdFromRecord(selfRecord);
        var packetBuffer = returnedResult.Buffer;
        var decryptedPacket = AESUtility.AesCtrDecrypt(selfNodeId[..16], packetBuffer[..16], packetBuffer[16..]);
        var staticHeader = StaticHeader.DecodeFromBytes(decryptedPacket);
        var packetNonce = staticHeader.Nonce;
        var selfNodeRecord = _identityManager.Record;
        var destNodeId = _sessionManager.GetHandshakeInteraction(packetNonce);
        
        if (destNodeId == null)
        {
            Console.WriteLine("Failed to get dest node id from packet nonce.");
            return;
        }
        
        var destNodeRecord = _tableManager.GetNodeEntry(destNodeId).Record;

        if (destNodeRecord == null)
        {
            Console.WriteLine("Could not find destination record.");
            return;
        }
        
        var destNodePubkey = destNodeRecord.GetEntry<EntrySecp256K1>(EnrContentKey.Secp256K1).Value;
        var challengeData = ByteArrayUtils.JoinByteArrays(returnedResult.Buffer.AsSpan()[..16], staticHeader.GetHeader());
        var cryptoSession = _sessionManager.GetSession(destNodeId, returnedResult.RemoteEndPoint);

        if (cryptoSession == null)
        {
            Console.Write("Creating new session with dest node id: " + Convert.ToHexString(destNodeId)[..10] + " " + returnedResult.RemoteEndPoint);
            cryptoSession = _sessionManager.CreateSession(SessionType.Initiator, destNodeId, returnedResult.RemoteEndPoint, challengeData);
        }
        else
        {
            Console.WriteLine("Session already exists with: " + Convert.ToHexString(destNodeId) + " " + returnedResult.RemoteEndPoint);
            cryptoSession.ChallengeData = challengeData;
        }

        var ephemeralPubkey = cryptoSession.EphemeralPublicKey;
        var idSignature = cryptoSession.GenerateIdSignature(challengeData, ephemeralPubkey, destNodeId);
        var maskingIv = RandomUtility.GenerateMaskingIv(PacketConstants.MaskingIvSize);
        var sharedSecret = cryptoSession.GenerateSharedSecret(destNodePubkey);
        var sessionKeys = SessionUtility.GenerateSessionKeys(sharedSecret, selfNodeId, destNodeId, challengeData);
        
        cryptoSession.CurrentSessionKeys = sessionKeys;
        
        var handshakePacket = PacketConstructor.ConstructHandshakePacket(idSignature, ephemeralPubkey, selfNodeId, destNodeId, maskingIv, selfNodeRecord);
        var message = _messageConstructor.ConstructMessage(MessageType.Ping, destNodeId);
        var messageAd = ByteArrayUtils.JoinByteArrays(maskingIv, handshakePacket.Result.Item2.GetHeader());
        var encryptedMessage = AESUtility.AesGcmEncrypt(sessionKeys.InitiatorKey, handshakePacket.Result.Item2.Nonce, message, messageAd);
        var finalPacket = ByteArrayUtils.JoinByteArrays(handshakePacket.Result.Item1, encryptedMessage);
        await connection.SendAsync(finalPacket, returnedResult.RemoteEndPoint);
        Console.Write(" => Sent HANDSHAKE packet with encrypted message. " + "\n");
    }
}