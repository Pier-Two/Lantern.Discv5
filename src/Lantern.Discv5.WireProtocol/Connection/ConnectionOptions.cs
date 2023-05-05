using System.Net;

namespace Lantern.Discv5.WireProtocol.Connection;

public class ConnectionOptions
{
    public int Port { get; }
    public IPAddress LocalIpAddress { get; }
    public IPAddress? ExternalIpAddress { get; }
    public int TimeoutMilliseconds { get; }
    public int LookupIntervalMilliseconds { get; }
    public int MaxRetryCount { get; }

    private ConnectionOptions(Builder builder)
    {
        Port = builder.Port;
        LocalIpAddress = builder.LocalIpAddress;
        ExternalIpAddress = builder.ExternalIpAddress;
        TimeoutMilliseconds = builder.TimeoutMilliseconds;
        LookupIntervalMilliseconds = builder.LookupIntervalMilliseconds;
        MaxRetryCount = builder.MaxRetryCount;
    }

    public class Builder
    {
        public int Port { get; private set; } = 9000;
        public IPAddress LocalIpAddress { get; private set; } = ConnectionUtility.GetLocalIpAddress();
        public IPAddress? ExternalIpAddress { get; private set; }
        public int TimeoutMilliseconds { get; private set; } = 2000;
        public int LookupIntervalMilliseconds { get; private set; } = 3000;
        public int MaxRetryCount { get; private set; } = 3;

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
            catch (Exception ex)
            {
                // Console.WriteLine("Failed to determine external IP address.");
            }
            
            return this;
        }

        public Builder WithTimeoutMilliseconds(int timeoutMilliseconds)
        {
            TimeoutMilliseconds = timeoutMilliseconds;
            return this;
        }

        public Builder WithLookupIntervalMilliseconds(int lookupIntervalMilliseconds)
        {
            LookupIntervalMilliseconds = lookupIntervalMilliseconds;
            return this;
        }

        public Builder WithMaxRetryCount(int maxRetryCount)
        {
            MaxRetryCount = maxRetryCount;
            return this;
        }

        public ConnectionOptions Build()
        {
            return new ConnectionOptions(this);
        }
    }
}