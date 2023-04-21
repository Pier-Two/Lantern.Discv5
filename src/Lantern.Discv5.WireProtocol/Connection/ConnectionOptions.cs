using System.Net;
using Lantern.Discv5.WireProtocol.Utility;

namespace Lantern.Discv5.WireProtocol.Connection;

public class ConnectionOptions
{
    public int Port { get; }
    public IPAddress IpAddress { get; }
    public int TimeoutMilliseconds { get; }
    public int LookupIntervalMilliseconds { get; }
    public int MaxRetryCount { get; }

    private ConnectionOptions(Builder builder)
    {
        IpAddress = builder.IpAddress;
        Port = builder.Port;
        TimeoutMilliseconds = builder.TimeoutMilliseconds;
        LookupIntervalMilliseconds = builder.LookupIntervalMilliseconds;
        MaxRetryCount = builder.MaxRetryCount;
    }

    public class Builder
    {
        public int Port { get; private set; } = 30303;
        public IPAddress IpAddress { get; private set; } = Networking.GetLocalIpAddress();
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
            IpAddress = ipAddress;
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