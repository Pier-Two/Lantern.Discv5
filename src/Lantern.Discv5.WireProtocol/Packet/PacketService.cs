using System.Net;
using System.Net.Sockets;
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
    private readonly IMessageRequester _messageRequester;
    private readonly IUdpConnection _udpConnection;
    private readonly ILookupManager _lookupManager;

    public PacketService(IPacketHandlerFactory packetHandlerFactory, IIdentityManager identityManager,
        ISessionManager sessionManager, ITableManager tableManager, IMessageRequester messageRequester,
        IUdpConnection udpConnection, ILookupManager lookupManager)
    {
        _packetHandlerFactory = packetHandlerFactory;
        _identityManager = identityManager;
        _sessionManager = sessionManager;
        _tableManager = tableManager;
        _messageRequester = messageRequester;
        _udpConnection = udpConnection;
        _lookupManager = lookupManager;
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
        if (_tableManager.RecordCount == 0)
        {
            Console.WriteLine("Initialising from bootstrap ENRs...\n");
            var bootstrapEnrs = _tableManager.GetBootstrapEnrs();

            foreach (var bootstrapEnr in bootstrapEnrs)
            {
                await SendPacket(MessageType.Ping, bootstrapEnr);
            }
            
            return;
        }
        
        await PerformDiscovery();
    }

    public async Task PingNodeAsync()
    {
        if (_tableManager.RecordCount > 0)
        {
            Console.WriteLine("Pinging node for checking liveness...");
            var targetNodeId = RandomUtility.GenerateNodeId(PacketConstants.NodeIdSize);
            var nodeEntry = _tableManager.GetInitialNodesForLookup(targetNodeId).First();
            await SendPacket(MessageType.Ping, nodeEntry.Record);
        }
    }
    
    public async Task PerformLookup(byte[] targetNodeId)
    {
        Console.WriteLine("\nPerforming lookup...");
        var closestNodes = await _lookupManager.PerformLookup(targetNodeId);
        Console.WriteLine("Lookup completed. Closest nodes found:");
        foreach (var node in closestNodes)
        {
            Console.WriteLine("Node ID: " + Convert.ToHexString(node.Id));
        }
    }

    private async Task PerformDiscovery()
    {
        Console.WriteLine("\nPerforming discovery...");
        var targetNodeId = RandomUtility.GenerateNodeId(PacketConstants.NodeIdSize);
        var initialNodesForLookup = _tableManager.GetInitialNodesForLookup(targetNodeId);
        
        // Establish sessions with initial nodes
        foreach (var nodeEntry in initialNodesForLookup)
        {
            if (!nodeEntry.IsQueried)
            {
                await SendPacket(MessageType.FindNode, nodeEntry.Record);
            }
        }
    }

    public async Task SendPacket(MessageType messageType, EnrRecord record)
    {
        var sourceNodeId = _identityManager.Verifier.GetNodeIdFromRecord(_identityManager.Record);
        var destNodeId = _identityManager.Verifier.GetNodeIdFromRecord(record);
        var destEndPoint = new IPEndPoint(record.GetEntry<EntryIp>(EnrContentKey.Ip).Value, record.GetEntry<EntryUdp>(EnrContentKey.Udp).Value);
        var maskingIv = RandomUtility.GenerateMaskingIv(PacketConstants.MaskingIvSize);
        var packetNonce = RandomUtility.GenerateNonce(PacketConstants.NonceSize);
        var cryptoSession = _sessionManager.GetSession(destNodeId, destEndPoint);

        if (cryptoSession is { IsEstablished: true })
        {
            var sessionKeys = cryptoSession.CurrentSessionKeys;
            var encryptionKey = cryptoSession.SessionType switch
            {
                SessionType.Initiator => sessionKeys.InitiatorKey,
                SessionType.Recipient => sessionKeys.RecipientKey,
                _ => throw new InvalidOperationException("Invalid session type")
            };
            
            var ordinaryPacket = PacketConstructor.ConstructOrdinaryPacket(sourceNodeId, destNodeId, maskingIv);
            var message = _messageRequester.ConstructMessage(messageType, destNodeId);
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