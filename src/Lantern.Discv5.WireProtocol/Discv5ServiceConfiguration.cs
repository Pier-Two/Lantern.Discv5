using Lantern.Discv5.Enr;
using Lantern.Discv5.WireProtocol.Connection;
using Lantern.Discv5.WireProtocol.Identity;
using Lantern.Discv5.WireProtocol.Message;
using Lantern.Discv5.WireProtocol.Packet;
using Lantern.Discv5.WireProtocol.Packet.Handlers;
using Lantern.Discv5.WireProtocol.Session;
using Lantern.Discv5.WireProtocol.Table;
using Lantern.Discv5.WireProtocol.Utility;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lantern.Discv5.WireProtocol;

public static class Discv5ServiceConfiguration
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

        AddLoggerServices(services, loggerFactory);
        AddConnectionServices(services, connectionOptions, sessionOptions, tableOptions, talkResponder);
        AddIdentityServices(services, enrEntryRegistry, enr);
        AddTableServices(services);
        AddPacketServices(services);
        AddMessageServices(services);
        AddSessionServices(services);
        AddUtilityServices(services);

        return services;
    }

    private static void AddLoggerServices(IServiceCollection services, ILoggerFactory loggerFactory)
    {
        services.AddSingleton(loggerFactory);
        services.AddSingleton(loggerFactory.CreateLogger<Discv5Protocol>());
    }

    private static void AddConnectionServices(IServiceCollection services, ConnectionOptions connectionOptions, SessionOptions sessionOptions, TableOptions tableOptions, ITalkReqAndRespHandler? talkResponder)
    {
        if (talkResponder != null)
            services.AddSingleton(talkResponder);
        
        services.AddSingleton<IConnectionManager, ConnectionManager>();
        services.AddSingleton(connectionOptions);
        services.AddSingleton(sessionOptions);
        services.AddSingleton(tableOptions);
        services.AddSingleton<IUdpConnection, UdpConnection>();
    }

    private static void AddIdentityServices(IServiceCollection services, IEnrEntryRegistry enrEntryRegistry, IEnr enr)
    {
        services.AddSingleton(enrEntryRegistry);
        services.AddSingleton(enr);
        services.AddSingleton<IEnrFactory, EnrFactory>();
        services.AddSingleton<IIdentityManager, IdentityManager>();
    }

    private static void AddTableServices(IServiceCollection services)
    {
        services.AddSingleton<IRoutingTable, RoutingTable>();
        services.AddSingleton<ITableManager, TableManager>();
        services.AddSingleton<ILookupManager, LookupManager>();
    }

    private static void AddPacketServices(IServiceCollection services)
    {
        services.AddSingleton<IPacketManager, PacketManager>();
        services.AddSingleton<IPacketBuilder, PacketBuilder>();
        services.AddSingleton<IPacketProcessor, PacketProcessor>();
        services.AddSingleton<IPacketHandlerFactory, PacketHandlerFactory>();
        services.AddTransient<OrdinaryPacketHandler>();
        services.AddTransient<WhoAreYouPacketHandler>();
        services.AddTransient<HandshakePacketHandler>();
    }

    private static void AddMessageServices(IServiceCollection services)
    {
        services.AddSingleton<IMessageDecoder, MessageDecoder>();
        services.AddSingleton<IMessageRequester, MessageRequester>();
        services.AddSingleton<IMessageResponder, MessageResponder>();
        services.AddSingleton<IRequestManager, RequestManager>();
    }

    private static void AddSessionServices(IServiceCollection services)
    {
        services.AddSingleton<IAesCrypto, AesCrypto>();
        services.AddSingleton<ISessionCrypto, SessionCrypto>();
        services.AddSingleton<ISessionManager, SessionManager>();
    }

    private static void AddUtilityServices(IServiceCollection services)
    {
        services.AddSingleton<Discv5Protocol>();
        services.AddSingleton<IGracefulTaskRunner, GracefulTaskRunner>();
        services.AddTransient<ICancellationTokenSourceWrapper, CancellationTokenSourceWrapper>();
        services.AddSingleton<IRoutingTable, RoutingTable>();
    }
}