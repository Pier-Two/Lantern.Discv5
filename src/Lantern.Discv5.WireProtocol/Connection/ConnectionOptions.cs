using System.Net;

namespace Lantern.Discv5.WireProtocol.Connection;

public class ConnectionOptions
{
    public int Port { get; private set; } = 9000;
    public IPAddress? IpAddress { get; private set; }
    public int ReceiveTimeoutMs { get; private set; } = 1000;
    public int RequestTimeoutMs { get; private set; } = 2000;
    public int CheckPendingRequestsDelayMs { get; private set; } = 500;
    public int RemoveCompletedRequestsDelayMs { get; private set; } = 1000;

    public static ConnectionOptions Default => new();

    public ConnectionOptions SetPort(int port)
    {
        Port = port;
        return this;
    }

    public ConnectionOptions SetIpAddress(IPAddress ipAddress)
    {
        IpAddress = ipAddress;
        return this;
    }

    public ConnectionOptions SetReceiveTimeoutMs(int receiveTimeoutMs)
    {
        ReceiveTimeoutMs = receiveTimeoutMs;
        return this;
    }

    public ConnectionOptions SetRequestTimeoutMs(int requestTimeoutMs)
    {
        RequestTimeoutMs = requestTimeoutMs;
        return this;
    }

    public ConnectionOptions SetCheckPendingRequestsDelayMs(int checkPendingRequestsDelayMs)
    {
        CheckPendingRequestsDelayMs = checkPendingRequestsDelayMs;
        return this;
    }

    public ConnectionOptions SetRemoveCompletedRequestsDelayMs(int removeCompletedRequestsDelayMs)
    {
        RemoveCompletedRequestsDelayMs = removeCompletedRequestsDelayMs;
        return this;
    }
}