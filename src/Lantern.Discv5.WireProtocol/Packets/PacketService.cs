using System.Net;
using System.Net.Sockets;
using Lantern.Discv5.Enr.EnrContent.Entries;
using Lantern.Discv5.WireProtocol.Connection;
using Lantern.Discv5.WireProtocol.Identity;
using Lantern.Discv5.WireProtocol.Packets.Handlers;
using Lantern.Discv5.WireProtocol.Packets.Headers;
using Lantern.Discv5.WireProtocol.Packets.Types;
using Lantern.Discv5.WireProtocol.Session;
using Lantern.Discv5.WireProtocol.Table;
using Lantern.Discv5.WireProtocol.Utility;

namespace Lantern.Discv5.WireProtocol.Packets;

public class PacketService : IPacketService
{
    private readonly IPacketHandlerFactory _packetHandlerFactory;
    private readonly IIdentityManager _identityManager;
    private readonly ISessionManager _sessionManager;
    private readonly ITableManager _tableManager;

    public PacketService(IPacketHandlerFactory packetHandlerFactory, IIdentityManager identityManager, ISessionManager sessionManager, ITableManager tableManager)
    {
        _packetHandlerFactory = packetHandlerFactory;
        _identityManager = identityManager;
        _sessionManager = sessionManager;
        _tableManager = tableManager;
        LogIdentityManagerDetails(); // For debugging purposes only (ignore for now)
    }
    
    public async Task SendOrdinaryPacketForLookup(IUdpConnection udpConnection)
    {
        var srcNodeId = _identityManager.Verifier.GetNodeIdFromRecord(_identityManager.Record);
        var destNode = _tableManager.Options.BootstrapEnrs[0];
        var destNodeId = _identityManager.Verifier.GetNodeIdFromRecord(destNode);
        var destEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 5500);
        var maskingIv = PacketUtils.GenerateMaskingIv();
        var packetNonce = PacketUtils.GenerateNonce();
        
        var result = _sessionManager.SaveHandshakeInteraction(packetNonce, destNodeId);
        var constructedOrdinaryPacket = PacketConstructor.ConstructOrdinaryPacket(srcNodeId, destNodeId, packetNonce, maskingIv);
        await udpConnection.SendAsync(constructedOrdinaryPacket.Result.Item1, destEndPoint);
        Console.WriteLine("Sent ordinary packet for lookup.");
    }
    
    public async Task HandleReceivedPacket(IUdpConnection connection, UdpReceiveResult returnedResult)
    {
        var decryptedPacket = DecryptPacket(returnedResult.Buffer);
        var staticHeader = StaticHeader.DecodeFromBytes(decryptedPacket);
        var packetHandler = _packetHandlerFactory.GetPacketHandler((PacketType)staticHeader.Flag);
        await packetHandler.HandlePacket(connection, returnedResult);
    }
    
    private byte[] DecryptPacket(byte[] packetBuffer)
    {
        var selfNodeId = _identityManager.Verifier.GetNodeIdFromRecord(_identityManager.Record);
        return AesUtils.AesCtrDecrypt(selfNodeId[..16], packetBuffer[..16], packetBuffer[16..]);
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