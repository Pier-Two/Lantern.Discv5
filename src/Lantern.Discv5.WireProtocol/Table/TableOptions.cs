namespace Lantern.Discv5.WireProtocol.Table;

public class TableOptions
{
    public int PingIntervalMilliseconds { get; private set; } = 5000;
    public int RefreshIntervalMilliseconds { get; private set; } = 300000;
    public int LookupTimeoutMilliseconds { get; private set; } = 10000;
    public int MaxAllowedFailures { get; private set; } = 3;
    public int ReplacementCacheSize { get; private set; } = 200;
    public int ConcurrencyParameter { get; private set; } = 3;
    public int LookupParallelism { get; private set; } = 2;
    public string[] BootstrapEnrs { get; private set; } = Array.Empty<string>();

    public static TableOptions Default => new TableOptions();

    public TableOptions SetPingIntervalMilliseconds(int pingIntervalMilliseconds)
    {
        PingIntervalMilliseconds = pingIntervalMilliseconds;
        return this;
    }

    public TableOptions SetRefreshIntervalMilliseconds(int refreshIntervalMilliseconds)
    {
        RefreshIntervalMilliseconds = refreshIntervalMilliseconds;
        return this;
    }

    public TableOptions SetLookupTimeoutMilliseconds(int lookupTimeoutMilliseconds)
    {
        LookupTimeoutMilliseconds = lookupTimeoutMilliseconds;
        return this;
    }

    public TableOptions SetMaxAllowedFailures(int maxAllowedFailures)
    {
        MaxAllowedFailures = maxAllowedFailures;
        return this;
    }

    public TableOptions SetReplacementCacheSize(int replacementCacheSize)
    {
        ReplacementCacheSize = replacementCacheSize;
        return this;
    }

    public TableOptions SetConcurrencyParameter(int concurrencyParameter)
    {
        ConcurrencyParameter = concurrencyParameter;
        return this;
    }

    public TableOptions SetLookupParallelism(int lookupParallelism)
    {
        LookupParallelism = lookupParallelism;
        return this;
    }

    public TableOptions SetBootstrapEnrs(string[] bootstrapEnrs)
    {
        BootstrapEnrs = bootstrapEnrs;
        return this;
    }
}