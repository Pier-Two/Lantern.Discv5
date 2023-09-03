using Discv5ConsoleApp.Lantern.Discv5.WireProtocol.Table;
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

public static class ServiceConfiguration
{
    public static IServiceCollection ConfigureServices(ILoggerFactory loggerFactory, 
        ConnectionOptions connectionOptions, SessionOptions sessionOptions, 
        TableOptions tableOptions, ITalkReqAndRespHandler? talkResponder = null)
    {
        var services = new ServiceCollection();
        
        services.AddSingleton(loggerFactory);
        services.AddSingleton(loggerFactory.CreateLogger<Discv5Protocol>());
        services.AddSingleton(connectionOptions);
        services.AddSingleton(sessionOptions);
        services.AddSingleton(tableOptions);
        services.AddSingleton(loggerFactory);
        
        if(talkResponder != null)
            services.AddSingleton(talkResponder);

        services.AddSingleton<ILookupManager, LookupManager>();
        services.AddSingleton<IPacketManager, PacketManager>();
        services.AddSingleton<IIdentityManager, IdentityManager>();
        services.AddSingleton<IUdpConnection, UdpConnection>();
        services.AddTransient<ICancellationTokenSourceWrapper, CancellationTokenSourceWrapper>();
        services.AddSingleton<IConnectionManager, ConnectionManager>();
        services.AddSingleton<IRequestManager, RequestManager>();
        services.AddSingleton<IRoutingTable, RoutingTable>();
        services.AddSingleton<ISessionCrypto, SessionCrypto>();
        services.AddSingleton<IGracefulTaskRunner, GracefulTaskRunner>();
        services.AddSingleton<IPacketBuilder, PacketBuilder>();
        services.AddSingleton<IPacketProcessor, PacketProcessor>();
        services.AddSingleton<IAesCrypto, AesCrypto>();
        services.AddSingleton<IEnrRecordFactory, EnrRecordFactory>();
        services.AddSingleton<IPacketHandlerFactory, PacketHandlerFactory>();
        services.AddSingleton<ISessionManager, SessionManager>();
        services.AddSingleton<ITableManager, TableManager>();
        services.AddSingleton<IRequestManager, RequestManager>();
        services.AddSingleton<IMessageDecoder, MessageDecoder>();
        services.AddSingleton<IMessageRequester, MessageRequester>();
        services.AddSingleton<IMessageResponder, MessageResponder>();

        services.AddTransient<OrdinaryPacketHandler>();
        services.AddTransient<WhoAreYouPacketHandler>();
        services.AddTransient<HandshakePacketHandler>();
        services.AddSingleton<Discv5Protocol>();

        return services;
    }
}