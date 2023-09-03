using Lantern.Discv5.WireProtocol.Connection;
using Lantern.Discv5.WireProtocol.Logging;
using Lantern.Discv5.WireProtocol.Message;
using Lantern.Discv5.WireProtocol.Session;
using Lantern.Discv5.WireProtocol.Table;
using Microsoft.Extensions.DependencyInjection;

namespace Lantern.Discv5.WireProtocol;

public static class Discv5Builder
{
    public static Discv5Protocol CreateDefault(string[] bootstrapEnrs, ITalkReqAndRespHandler? talkResponder = null)
    {
        var connectionOptions = ConnectionOptions.Default;
        var sessionOptions = SessionOptions.Default;
        var tableOptions = new TableOptions.Builder()
            .WithBootstrapEnrs(bootstrapEnrs)
            .Build();
        var loggerFactory = LoggingOptions.Default;
        var services = ServiceConfiguration.ConfigureServices(
            loggerFactory, connectionOptions, sessionOptions, tableOptions, talkResponder
        );
        var serviceProvider = services.BuildServiceProvider();
        
        return serviceProvider.GetRequiredService<Discv5Protocol>();
    }
}