using System.Net;

namespace Lantern.Discv5.WireProtocol.Connection;

public class ConnectionOptions
{
    public int Port { get; }
    public IPAddress LocalIpAddress { get; }
    public IPAddress? ExternalIpAddress { get; }
    public int RequestTimeoutMs { get; }
    public int CheckPendingRequestsDelayMs { get; }
    public int RemoveCompletedRequestsDelayMs { get; }

    private ConnectionOptions(Builder builder)
    {
        Port = builder.Port;
        LocalIpAddress = builder.LocalIpAddress;
        ExternalIpAddress = builder.ExternalIpAddress;
        RequestTimeoutMs = builder.TimeoutMilliseconds;
        CheckPendingRequestsDelayMs = builder.CheckPendingRequestsDelayMs;
        RemoveCompletedRequestsDelayMs = builder.RemoveCompletedRequestsDelayMs;
    }

    public class Builder
    {
        public int Port { get; private set; } = 9000;
        public IPAddress LocalIpAddress { get; private set; } = ConnectionUtility.GetLocalIpAddress();
        public IPAddress? ExternalIpAddress { get; private set; }
        public int TimeoutMilliseconds { get; private set; } = 1000;
        public int CheckPendingRequestsDelayMs { get; private set; } = 500;
        public int RemoveCompletedRequestsDelayMs { get; private set; } = 3000;
        
        public Builder WithPort(int port)
        {
            Port = port;
            return this;
        }

        public Builder WithIpAddress(IPAddress ipAddress)
        {
            LocalIpAddress = ipAddress;
            return this;
        }

        public async Task<Builder> WithExternalIpAddressAsync()
        {
            try
            {
                ExternalIpAddress = await ConnectionUtility.DetermineExternalIpAddress();
            }
            catch
            {
                // ignored
            }

            return this;
        }

        public Builder WithTimeoutMilliseconds(int timeoutMilliseconds)
        {
            TimeoutMilliseconds = timeoutMilliseconds;
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