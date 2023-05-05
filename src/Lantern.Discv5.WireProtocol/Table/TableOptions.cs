using Lantern.Discv5.Enr;

namespace Lantern.Discv5.WireProtocol.Table;

public class TableOptions
{
    public int MaxReplacementCacheSize { get; }
    
    public EnrRecord[] BootstrapEnrs { get; }

    private TableOptions(Builder builder)
    {
        MaxReplacementCacheSize = builder.MaxReplacementCacheSize;
        BootstrapEnrs = builder.BootstrapEnrs;
    }

    public class Builder
    {
        public int MaxReplacementCacheSize { get; private set; } = 3;
        
        public EnrRecord[] BootstrapEnrs { get; private set; } = Array.Empty<EnrRecord>();

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