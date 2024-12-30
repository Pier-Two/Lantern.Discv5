using System.Net;

namespace Lantern.Discv5.WireProtocol.Connection;

public class ConnectionOptions
{
    public IPAddress? IpAddress { get; set; }
    public int UdpPort { get; set; } = 30303;
    public int ReceiveTimeoutMs { get; set; } = 10000;
    public int RequestTimeoutMs { get; set; } = 20000;
    public int CheckPendingRequestsDelayMs { get; set; } = 500;
    public int RemoveCompletedRequestsDelayMs { get; set; } = 1000;
}
