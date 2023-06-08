using Lantern.Discv5.Enr;
using Lantern.Discv5.Enr.EnrFactory;

namespace Lantern.Discv5.WireProtocol.Table;

public class TableOptions
{
    public int PingIntervalMilliseconds { get; }
    public int RefreshIntervalMilliseconds { get; }
    public int LookupIntervalMilliseconds { get; }
    public int LookupTimeoutMilliseconds { get; }
    public int MaxAllowedFailures { get; }
    public int ConcurrencyParameter { get; }
    public int LookupParallelism { get; }
    public EnrRecord[] BootstrapEnrs { get; }
    
    public static TableOptions Default => new Builder().Build();

    private TableOptions(Builder builder)
    {
        PingIntervalMilliseconds = builder.PingIntervalMilliseconds;
        RefreshIntervalMilliseconds = builder.RefreshIntervalMilliseconds;
        LookupIntervalMilliseconds = builder.LookupIntervalMilliseconds;
        LookupTimeoutMilliseconds = builder.LookupTimeoutMilliseconds;
        MaxAllowedFailures = builder.MaxAllowedFailures;
        ConcurrencyParameter = builder.ConcurrencyParameter;
        LookupParallelism = builder.LookupParallelism;
        BootstrapEnrs = builder.BootstrapEnrs;
    }

    public class Builder
    {
        public int PingIntervalMilliseconds { get; private set; } = 5000;
        public int RefreshIntervalMilliseconds { get; private set; } = 5000;
        public int LookupIntervalMilliseconds { get; private set; } = 3000;
        public int LookupTimeoutMilliseconds { get; private set; } = 10000;
        public int MaxAllowedFailures { get; private set; } = 3;
        public int ConcurrencyParameter { get; private set; } = 3;
        public int LookupParallelism { get; private set; } = 2;
        public EnrRecord[] BootstrapEnrs { get; private set; } = Array.Empty<EnrRecord>();
        
        public Builder WithPingIntervalMilliseconds(int pingIntervalMilliseconds)
        {
            PingIntervalMilliseconds = pingIntervalMilliseconds;
            return this;
        }
        
        public Builder WithRefreshIntervalMilliseconds(int refreshIntervalMilliseconds)
        {
            RefreshIntervalMilliseconds = refreshIntervalMilliseconds;
            return this;
        }

        public Builder WithLookupIntervalMilliseconds(int lookupIntervalMilliseconds)
        {
            LookupIntervalMilliseconds = lookupIntervalMilliseconds;
            return this;
        }
        
        public Builder WithLookupTimeoutMilliseconds(int lookupTimeoutMilliseconds)
        {
            LookupTimeoutMilliseconds = lookupTimeoutMilliseconds;
            return this;
        }
        
        public Builder WithMaxAllowedFailures(int maxAllowedFailures)
        {
            MaxAllowedFailures = maxAllowedFailures;
            return this;
        }
        
        public Builder WithConcurrencyParameter(int concurrencyParameter)
        {
            ConcurrencyParameter = concurrencyParameter;
            return this;
        }
        
        public Builder WithLookupParallelism(int lookupParallelism)
        {
            LookupParallelism = lookupParallelism; 
            return this;
        }

        public Builder WithBootstrapEnrs(IEnumerable<EnrRecord> bootstrapEnrs)
        {
            BootstrapEnrs = bootstrapEnrs.ToArray();
            return this;
        }

        public TableOptions Build(string[]? bootstrapEnrs = null)
        {
            if (bootstrapEnrs != null)
            {
                BootstrapEnrs = GetBootstrapEnrRecords(bootstrapEnrs);
            }

            return new TableOptions(this);
        }
        
        private static EnrRecord[] GetBootstrapEnrRecords(string[] bootstrapEnrs)
        {
            return bootstrapEnrs
                .Select(enr => new EnrRecordFactory().CreateFromString(enr))
                .ToArray();
        }
    }
}