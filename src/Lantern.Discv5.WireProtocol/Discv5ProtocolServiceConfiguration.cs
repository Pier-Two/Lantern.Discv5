using Lantern.Discv5.Enr;
using Lantern.Discv5.WireProtocol.Connection;
using Lantern.Discv5.WireProtocol.Identity;
using Lantern.Discv5.WireProtocol.Messages;
using Lantern.Discv5.WireProtocol.Packet;
using Lantern.Discv5.WireProtocol.Packet.Handlers;
using Lantern.Discv5.WireProtocol.Session;
using Lantern.Discv5.WireProtocol.Table;
using Lantern.Discv5.WireProtocol.Utility;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Lantern.Discv5.WireProtocol;

public static class Discv5ProtocolServiceConfiguration
{
    internal static IServiceCollection AddDiscv5(
        this IServiceCollection services,
        TableOptions tableOptions,
        ConnectionOptions connectionOptions,
        SessionOptions sessionOptions,
        IEnrEntryRegistry enrEntryRegistry,
        IEnr enr,
        ILoggerFactory loggerFactory,
        ITalkReqAndRespHandler? talkResponder = null)
    {
        ValidateMandatoryConfigurations(tableOptions, connectionOptions, sessionOptions, enrEntryRegistry, enr, loggerFactory);

        AddLoggerServices(services, loggerFactory);
        AddConnectionServices(services, connectionOptions, sessionOptions, tableOptions, talkResponder);
        AddIdentityServices(services, enrEntryRegistry, enr);
        AddTableServices(services);
        AddPacketServices(services);
        AddMessageServices(services);
        AddSessionServices(services);
        AddUtilityServices(services);

        services.AddSingleton<IDiscv5Protocol, Discv5Protocol>();

        return services;
    }

    private static void ValidateMandatoryConfigurations(
        TableOptions tableOptions,
        ConnectionOptions connectionOptions,
        SessionOptions sessionOptions,
        IEnrEntryRegistry enrEntryRegistry,
        IEnr enr,
        ILoggerFactory loggerFactory)
    {
        if (loggerFactory == null || connectionOptions == null || sessionOptions == null || enrEntryRegistry == null || enr == null || tableOptions == null)
        {
            throw new InvalidOperationException("Missing mandatory configurations.");
        }
    }

    private static void AddLoggerServices(IServiceCollection services, ILoggerFactory loggerFactory)
    {
        services.AddSingleton(loggerFactory);
        services.AddSingleton(loggerFactory.CreateLogger<IDiscv5Protocol>());
    }

    private static void AddConnectionServices(IServiceCollection services, ConnectionOptions connectionOptions, SessionOptions sessionOptions, TableOptions tableOptions, ITalkReqAndRespHandler? talkResponder)
    {
        if (talkResponder != null)
            services.TryAddSingleton(talkResponder);

        services.AddSingleton(connectionOptions);
        services.AddSingleton(sessionOptions);
        services.AddSingleton(tableOptions);
        services.TryAddSingleton<IConnectionManager, ConnectionManager>();
        services.TryAddSingleton<IUdpConnection, UdpConnection>();
    }

    private static void AddIdentityServices(IServiceCollection services, IEnrEntryRegistry enrEntryRegistry, IEnr enr)
    {
        services.AddSingleton(enrEntryRegistry);
        services.AddSingleton(enr);
        services.TryAddSingleton<IEnrFactory, EnrFactory>();
        services.TryAddSingleton<IIdentityManager, IdentityManager>();
    }

    private static void AddTableServices(IServiceCollection services)
    {
        services.TryAddSingleton<IRoutingTable, RoutingTable>();
        services.TryAddSingleton<ITableManager, TableManager>();
        services.TryAddSingleton<ILookupManager, LookupManager>();
    }

    private static void AddPacketServices(IServiceCollection services)
    {
        services.TryAddSingleton<IPacketManager, PacketManager>();
        services.TryAddSingleton<IPacketBuilder, PacketBuilder>();
        services.TryAddSingleton<IPacketProcessor, PacketProcessor>();
        services.TryAddSingleton<IPacketReceiver, PacketReceiver>();
        services.TryAddSingleton<IPacketHandlerFactory, PacketHandlerFactory>();
        services.TryAddTransient<OrdinaryPacketHandler>();
        services.TryAddTransient<WhoAreYouPacketHandler>();
        services.TryAddTransient<HandshakePacketHandler>();
    }

    private static void AddMessageServices(IServiceCollection services)
    {
        services.TryAddSingleton<IMessageDecoder, MessageDecoder>();
        services.TryAddSingleton<IMessageRequester, MessageRequester>();
        services.TryAddSingleton<IMessageResponder, MessageResponder>();
        services.TryAddSingleton<IRequestManager, RequestManager>();
    }

    private static void AddSessionServices(IServiceCollection services)
    {
        services.TryAddSingleton<IAesCrypto, AesCrypto>();
        services.TryAddSingleton<ISessionCrypto, SessionCrypto>();
        services.TryAddSingleton<ISessionManager, SessionManager>();
    }

    private static void AddUtilityServices(IServiceCollection services)
    {
        services.TryAddSingleton<IGracefulTaskRunner, GracefulTaskRunner>();
        services.TryAddTransient<ICancellationTokenSourceWrapper, CancellationTokenSourceWrapper>();
        services.TryAddSingleton<IRoutingTable, RoutingTable>();
    }
}
