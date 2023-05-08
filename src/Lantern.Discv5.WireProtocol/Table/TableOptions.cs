using Lantern.Discv5.Enr;

namespace Lantern.Discv5.WireProtocol.Table;

public class TableOptions
{
    public int MaxAllowedFailures { get; }
    
    public int LookupConcurrency { get; }
    
    public int MaxReplacementCacheSize { get; }
    
    public EnrRecord[] BootstrapEnrs { get; }

    private TableOptions(Builder builder)
    {
        MaxAllowedFailures = builder.MaxAllowedFailures;
        LookupConcurrency = builder.LookupConcurrency;
        MaxReplacementCacheSize = builder.MaxReplacementCacheSize;
        BootstrapEnrs = builder.BootstrapEnrs;
    }

    public class Builder
    {
        public int MaxAllowedFailures { get; private set; } = 3;
        
        public int LookupConcurrency { get; private set; } = 3;
        
        public int MaxReplacementCacheSize { get; private set; } = 3;
        
        public EnrRecord[] BootstrapEnrs { get; private set; } = Array.Empty<EnrRecord>();
        
        public Builder WithMaxAllowedFailures(int maxAllowedFailures)
        {
            MaxAllowedFailures = maxAllowedFailures;
            return this;
        }
        
        public Builder WithLookupConcurrency(int lookupConcurrency)
        {
            LookupConcurrency = lookupConcurrency;
            return this;
        }

        public Builder WithMaxReplacementCacheSize(int maxReplacementCacheSize)
        {
            MaxReplacementCacheSize = maxReplacementCacheSize;
            return this;
        }
        
        public Builder WithBootstrapEnrs(IEnumerable<EnrRecord> bootstrapEnrs)
        {
            BootstrapEnrs = bootstrapEnrs.ToArray();
            return this;
        }

        public TableOptions Build()
        {
            return new TableOptions(this);
        }
    }
}