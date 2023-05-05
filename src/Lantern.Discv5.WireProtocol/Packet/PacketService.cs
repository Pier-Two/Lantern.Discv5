using System.Net;
using System.Net.Sockets;
using System.Timers;
using Lantern.Discv5.Enr;
using Lantern.Discv5.Enr.EnrContent;
using Lantern.Discv5.Enr.EnrContent.Entries;
using Lantern.Discv5.Rlp;
using Lantern.Discv5.WireProtocol.Connection;
using Lantern.Discv5.WireProtocol.Identity;
using Lantern.Discv5.WireProtocol.Message;
using Lantern.Discv5.WireProtocol.Packet.Handlers;
using Lantern.Discv5.WireProtocol.Packet.Headers;
using Lantern.Discv5.WireProtocol.Packet.Types;
using Lantern.Discv5.WireProtocol.Session;
using Lantern.Discv5.WireProtocol.Table;

namespace Lantern.Discv5.WireProtocol.Packet;

public class PacketService : IPacketService
{
    private readonly IPacketHandlerFactory _packetHandlerFactory;
    private readonly IIdentityManager _identityManager;
    private readonly ISessionManager _sessionManager;
    private readonly ITableManager _tableManager;
    private readonly IMessageConstructor _messageConstructor;
    private readonly IUdpConnection _udpConnection;

    public PacketService(IPacketHandlerFactory packetHandlerFactory, IIdentityManager identityManager,
        ISessionManager sessionManager, ITableManager tableManager, IMessageConstructor messageConstructor,
        IUdpConnection udpConnection)
    {
        _packetHandlerFactory = packetHandlerFactory;
        _identityManager = identityManager;
        _sessionManager = sessionManager;
        _tableManager = tableManager;
        _messageConstructor = messageConstructor;
        _udpConnection = udpConnection;
        LogIdentityManagerDetails(); // For debugging purposes only (ignore for now)
    }

    private void LogIdentityManagerDetails()
    {
        Console.WriteLine("\nIDENTITY DETAILS");
        Console.WriteLine("================");
        Console.WriteLine("Ethereum Node Record: " + _identityManager.Record);
        Console.WriteLine("\nCOMMUNICATION LOGS");
        Console.WriteLine("==================");
    }

    public async Task RunDiscoveryAsync()
    {
        var sourceNodeId = _identityManager.Verifier.GetNodeIdFromRecord(_identityManager.Record);
        
        if (_tableManager.RecordCount == 0)
        {
            await DiscoverFromBootstrapEnrsAsync();
            await PerformLookup(sourceNodeId, sourceNodeId);
            return;
        }
        await PerformLookup(sourceNodeId, RandomUtility.GenerateNodeId(PacketConstants.NodeIdSize));
    }

    private async Task DiscoverFromBootstrapEnrsAsync()
    {
        Console.WriteLine("Starting discovery from bootstrap ENRs...\n");
        var bootstrapEnrs = _tableManager.GetBootstrapEnrs();
        var sourceNodeId = _identityManager.Verifier.GetNodeIdFromRecord(_identityManager.Record);

        foreach (var bootstrapEnr in bootstrapEnrs)
        {
            await SendPacket(MessageType.Ping, sourceNodeId, bootstrapEnr);
        }
    }

    private async Task PerformLookup(byte[] sourceNodeId, byte[] targetNodeId)
    {
        Console.WriteLine("\nPerforming lookup...");
        var initialNodesForLookup = _tableManager.GetInitialNodesForLookup(targetNodeId);

        foreach (var nodeEntry in initialNodesForLookup)
        {
            await SendPacket(MessageType.FindNode, sourceNodeId, nodeEntry.Record);
        }
    }

    private async Task SendPacket(MessageType messageType, byte[] sourceNodeId, EnrRecord record)
    {
        var destNodeId = _identityManager.Verifier.GetNodeIdFromRecord(record);
        var ipAddress = record.GetEntry<EntryIp>(EnrContentKey.Ip).Value;
        var port = record.GetEntry<EntryUdp>(EnrContentKey.Udp).Value;
        var destEndPoint = new IPEndPoint(ipAddress, port);
        var maskingIv = RandomUtility.GenerateMaskingIv(PacketConstants.MaskingIvSize);
        var packetNonce = RandomUtility.GenerateNonce(PacketConstants.NonceSize);
        var cryptoSession = _sessionManager.GetSession(destNodeId, destEndPoint);

        if (cryptoSession is { IsEstablished: true })
        {
            var sessionKeys = cryptoSession.CurrentSessionKeys;
            var encryptionKey = cryptoSession.SessionType == SessionType.Initiator
                ? sessionKeys.InitiatorKey
                : sessionKeys.RecipientKey;
            var ordinaryPacket = PacketConstructor.ConstructOrdinaryPacket(sourceNodeId, destNodeId, maskingIv);
            var message = _messageConstructor.ConstructMessage(messageType, destNodeId);
            var messageAd = ByteArrayUtils.JoinByteArrays(maskingIv, ordinaryPacket.Result.Item2.GetHeader());
            var encryptedMessage =
                AESUtility.AesGcmEncrypt(encryptionKey, ordinaryPacket.Result.Item2.Nonce, message, messageAd);
            var finalPacket = ByteArrayUtils.JoinByteArrays(ordinaryPacket.Result.Item1, encryptedMessage);
            await _udpConnection.SendAsync(finalPacket, destEndPoint);
            Console.WriteLine("Sent FINDNODES request to " + destEndPoint);
        }
        else
        {
            _sessionManager.SaveHandshakeInteraction(packetNonce, destNodeId);
            var constructedOrdinaryPacket =
                PacketConstructor.ConstructRandomOrdinaryPacket(sourceNodeId, destNodeId, packetNonce, maskingIv);
            await _udpConnection.SendAsync(constructedOrdinaryPacket.Result.Item1, destEndPoint);
            Console.WriteLine("Sent RANDOM packet to initiate handshake with " + destEndPoint);
        }
    }


    public async Task HandleReceivedPacket(UdpReceiveResult returnedResult)
    {
        var decryptedPacket = DecryptPacket(returnedResult.Buffer);
        var staticHeader = StaticHeader.DecodeFromBytes(decryptedPacket);
        var packetHandler = _packetHandlerFactory.GetPacketHandler((PacketType)staticHeader.Flag);
        await packetHandler.HandlePacket(_udpConnection, returnedResult);
    }

    private byte[] DecryptPacket(byte[] packetBuffer)
    {
        var selfNodeId = _identityManager.Verifier.GetNodeIdFromRecord(_identityManager.Record);
        return AESUtility.AesCtrDecrypt(selfNodeId[..16], packetBuffer[..16], packetBuffer[16..]);
    }
}