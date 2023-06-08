using Lantern.Discv5.WireProtocol.Connection;
using Lantern.Discv5.WireProtocol.Logging;
using Lantern.Discv5.WireProtocol.Session;
using Lantern.Discv5.WireProtocol.Table;
using Microsoft.Extensions.DependencyInjection;

namespace Lantern.Discv5.WireProtocol;

public static class Discv5Builder
{
    public static Discv5Protocol CreateDefault(string[] bootstrapEnrs)
    {
        var connectionOptions = ConnectionOptions.Default;
        var sessionOptions = SessionOptions.Default;
        var tableOptions = new TableOptions.Builder().Build(bootstrapEnrs);
        var loggerFactory = LoggingOptions.Default;
        var services = ServiceConfiguration.ConfigureServices(
            loggerFactory, connectionOptions, sessionOptions, tableOptions
        );
        var serviceProvider = services.BuildServiceProvider();
        
        return new Discv5Protocol(serviceProvider);
    }
}