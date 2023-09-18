using Lantern.Discv5.Enr;
using Lantern.Discv5.WireProtocol.Connection;
using Lantern.Discv5.WireProtocol.Message;
using Lantern.Discv5.WireProtocol.Session;
using Lantern.Discv5.WireProtocol.Table;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lantern.Discv5.WireProtocol.Services;

public static class ServiceConfiguration
{
    public static IServiceCollection ConfigureServices(
        ILoggerFactory loggerFactory,
        ConnectionOptions connectionOptions,
        SessionOptions sessionOptions,
        IEnrEntryRegistry enrEntryRegistry,
        IEnr enr,
        TableOptions tableOptions,
        ITalkReqAndRespHandler? talkResponder = null)
    {
        var services = new ServiceCollection();

        services.AddLoggerServices(loggerFactory)
            .AddConnectionServices(connectionOptions, sessionOptions, tableOptions, talkResponder)
            .AddIdentityServices(enrEntryRegistry, enr)
            .AddTableServices()
            .AddPacketServices()
            .AddMessageServices()
            .AddSessionServices()
            .AddUtilityServices();

        return services;
    }
}