using System.Net.Sockets;
using Lantern.Discv5.Enr;

namespace Lantern.Discv5.WireProtocol.Packet;

public interface IPacketManager
{
    Task SendPingPacket(EnrRecord destRecord);

    Task SendFindNodePacket( EnrRecord destRecord, byte[] targetNodeId);

    Task SendTalkReqPacket(EnrRecord destRecord, byte[] protocol, byte[] request);
    
    Task HandleReceivedPacket(UdpReceiveResult returnedResult);
}