using Lantern.Discv5.WireProtocol.Connection;
using Lantern.Discv5.WireProtocol.Message;
using Lantern.Discv5.WireProtocol.Session;
using Lantern.Discv5.WireProtocol.Table;
using Microsoft.Extensions.DependencyInjection;

namespace Lantern.Discv5.WireProtocol.Services;

public static class ConnectionService
{
    public static IServiceCollection AddConnectionServices(
        this IServiceCollection services,
        ConnectionOptions connectionOptions,
        SessionOptions sessionOptions,
        TableOptions tableOptions,
        ITalkReqAndRespHandler? talkResponder = null)
    {
        if (talkResponder != null)
            services.AddSingleton(talkResponder);
        
        services.AddSingleton<IConnectionManager, ConnectionManager>();
        services.AddSingleton(connectionOptions);
        services.AddSingleton(sessionOptions);
        services.AddSingleton(tableOptions);
        services.AddSingleton<IUdpConnection, UdpConnection>();

        return services;
    }
}