using System.Net.Sockets;
using Lantern.Discv5.Enr;

namespace Lantern.Discv5.WireProtocol.Packet;

public interface IPacketManager
{
    Task SendPingPacket(EnrRecord destRecord);

    Task SendFindNodePacket( EnrRecord destRecord, byte[] targetNodeId, bool varyDistance);
    
    Task HandleReceivedPacket(UdpReceiveResult returnedResult);
}