using Lantern.Discv5.Enr;

namespace Lantern.Discv5.WireProtocol.Table;

public class TableOptions
{
    public int BucketSize { get; }
    public int BucketCount { get; }
    public EnrRecord[] BootstrapEnrs { get; }

    private TableOptions(Builder builder)
    {
        BucketSize = builder.BucketSize;
        BucketCount = builder.BucketCount;
        BootstrapEnrs = builder.BootstrapEnrs;
    }

    public class Builder
    {
        public int BucketSize { get; private set; } = 16;
        public int BucketCount { get; private set; } = 256;
        public EnrRecord[] BootstrapEnrs { get; private set; } = Array.Empty<EnrRecord>();

        public Builder WithBucketSize(int bucketSize)
        {
            BucketSize = bucketSize;
            return this;
        }

        public Builder WithBucketCount(int bucketCount)
        {
            BucketCount = bucketCount;
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