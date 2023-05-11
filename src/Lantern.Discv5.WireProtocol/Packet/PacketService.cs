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
using Lantern.Discv5.WireProtocol.Utility;

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
    private readonly IAesUtility _aesUtility;
    private readonly IPacketBuilder _packetBuilder;

    public PacketService(IPacketHandlerFactory packetHandlerFactory, IIdentityManager identityManager,
        ISessionManager sessionManager, ITableManager tableManager, IMessageRequester messageRequester,
        IUdpConnection udpConnection, ILookupManager lookupManager, IAesUtility aesUtility, IPacketBuilder packetBuilder)
    {
        _packetHandlerFactory = packetHandlerFactory;
        _identityManager = identityManager;
        _sessionManager = sessionManager;
        _tableManager = tableManager;
        _messageRequester = messageRequester;
        _udpConnection = udpConnection;
        _lookupManager = lookupManager;
        _aesUtility = aesUtility;
        _packetBuilder = packetBuilder;
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
                try
                {
                    await SendPacket(MessageType.Ping, bootstrapEnr);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
               
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
        var destNodeId = _identityManager.Verifier.GetNodeIdFromRecord(record);
        var destEndPoint = new IPEndPoint(record.GetEntry<EntryIp>(EnrContentKey.Ip).Value, record.GetEntry<EntryUdp>(EnrContentKey.Udp).Value);
        var cryptoSession = _sessionManager.GetSession(destNodeId, destEndPoint);
        
        if (cryptoSession is { IsEstablished: true })
        {
            await SendOrdinaryPacketAsync(messageType, cryptoSession, destEndPoint, destNodeId);
            return;
        }

        await SendRandomOrdinaryPacketAsync(destEndPoint, destNodeId);
    }
    
    public async Task HandleReceivedPacket(UdpReceiveResult returnedResult)
    {
        var packet = new PacketMain(_identityManager, _aesUtility, returnedResult.Buffer);
        var packetHandler = _packetHandlerFactory.GetPacketHandler((PacketType)packet.StaticHeader.Flag);
        await packetHandler.HandlePacket(_udpConnection, returnedResult);
    }

    private async Task SendOrdinaryPacketAsync(MessageType messageType, SessionMain sessionMain, IPEndPoint destEndPoint, byte[] destNodeId)
    {
        var maskingIv = RandomUtility.GenerateMaskingIv(PacketConstants.MaskingIvSize);
        var ordinaryPacket = _packetBuilder.BuildOrdinaryPacket(destNodeId, maskingIv, sessionMain.MessageCount);
        var message = _messageRequester.ConstructMessage(messageType, destNodeId);

        if (message == null)
        {
            Console.WriteLine("Unable to construct PING message. Cannot send PING packet.");
            return;
        }
        
        var encryptedMessage = sessionMain.EncryptMessage(ordinaryPacket.Item2, maskingIv, message);
        var finalPacket = ByteArrayUtils.JoinByteArrays(ordinaryPacket.Item1, encryptedMessage);
        
        await _udpConnection.SendAsync(finalPacket, destEndPoint);
        Console.WriteLine("Sent FINDNODES request to " + destEndPoint);
    }

    private async Task SendRandomOrdinaryPacketAsync(IPEndPoint destEndPoint, byte[] destNodeId)
    {
        var maskingIv = RandomUtility.GenerateMaskingIv(PacketConstants.MaskingIvSize);
        var packetNonce = RandomUtility.GenerateNonce(PacketConstants.NonceSize);
            
        _sessionManager.SaveHandshakeInteraction(packetNonce, destNodeId);
            
        var constructedOrdinaryPacket = _packetBuilder.BuildRandomOrdinaryPacket(destNodeId, packetNonce, maskingIv);
        await _udpConnection.SendAsync(constructedOrdinaryPacket.Item1, destEndPoint);
        Console.WriteLine("Sent RANDOM packet to initiate handshake with " + destEndPoint);
    }
}