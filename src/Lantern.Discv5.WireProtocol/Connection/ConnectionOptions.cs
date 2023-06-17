using System.Net;

namespace Lantern.Discv5.WireProtocol.Connection;

public class ConnectionOptions
{
    public int Port { get; }
    public IPAddress? IpAddress { get; }
    public int ReqRespTimeoutMs { get; }
    public int PendingRequestTimeoutMs { get; }
    public int CheckPendingRequestsDelayMs { get; }
    public int RemoveCompletedRequestsDelayMs { get; }

    private ConnectionOptions(Builder builder)
    {
        Port = builder.Port;
        IpAddress = builder.IpAddress;
        ReqRespTimeoutMs = builder.ReqRespTimeoutMs;
        PendingRequestTimeoutMs = builder.PendingRequestTimeoutMs;
        CheckPendingRequestsDelayMs = builder.CheckPendingRequestsDelayMs;
        RemoveCompletedRequestsDelayMs = builder.RemoveCompletedRequestsDelayMs;
    }
    
    public static ConnectionOptions Default => new Builder().Build();

    public class Builder
    {
        public int Port { get; private set; } = 9000;
        public IPAddress? IpAddress { get; private set; }
        public int ReqRespTimeoutMs { get; private set; } = 1000;
        public int PendingRequestTimeoutMs { get; private set; } = 1000;
        public int CheckPendingRequestsDelayMs { get; private set; } = 500;
        public int RemoveCompletedRequestsDelayMs { get; private set; } = 3000;
        
        public Builder WithPort(int port)
        {
            Port = port;
            return this;
        }

        public Builder WithIpAddress(IPAddress ipAddress)
        {
            IpAddress = ipAddress;
            return this;
        }
        
        public Builder WithReqRespTimeoutMs(int reqRespTimeoutMs)
        {
            ReqRespTimeoutMs = reqRespTimeoutMs;
            return this;
        }

        public Builder WithPendingRequestTimeoutMs(int pendingRequestTimeoutMs)
        {
            PendingRequestTimeoutMs = pendingRequestTimeoutMs;
            return this;
        }
        
        public Builder WithCheckPendingRequestsDelayMs(int checkPendingRequestsDelayMs)
        {
            CheckPendingRequestsDelayMs = checkPendingRequestsDelayMs;
            return this;
        }
        
        public Builder WithRemoveFulfilledRequestsDelayMs(int removeFulfilledRequestsDelayMs)
        {
            RemoveCompletedRequestsDelayMs = removeFulfilledRequestsDelayMs;
            return this;
        }

        public ConnectionOptions Build()
        {
            return new ConnectionOptions(this);
        }
    }
}