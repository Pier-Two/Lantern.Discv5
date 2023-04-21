using Lantern.Discv5.WireProtocol.Connection;
using Lantern.Discv5.WireProtocol.Identity;
using Lantern.Discv5.WireProtocol.Messages;
using Lantern.Discv5.WireProtocol.Packets;
using Lantern.Discv5.WireProtocol.Packets.Handlers;
using Lantern.Discv5.WireProtocol.Session;
using Lantern.Discv5.WireProtocol.Table;
using Microsoft.Extensions.DependencyInjection;

namespace Lantern.Discv5.WireProtocol;

public static class ServiceConfiguration
{
    public static IServiceCollection ConfigureServices(ConnectionOptions connectionOptions, SessionOptions sessionOptions, TableOptions tableOptions)
    {
        var services = new ServiceCollection();

        services.AddSingleton(connectionOptions);
        services.AddSingleton(sessionOptions);
        services.AddSingleton(tableOptions);
        
        services.AddSingleton<IPacketService, PacketService>();
        services.AddSingleton<IUdpConnection, UdpConnection>();
        services.AddSingleton<ConnectionService>();

        services.AddSingleton<IPacketHandlerFactory, PacketHandlerFactory>();
        services.AddSingleton<IIdentityManager, IdentityManager>();
        services.AddSingleton<ISessionManager, SessionManager>();
        services.AddSingleton<ITableManager, TableManager>();
        services.AddSingleton<IMessageHandler, MessageHandler>();

        services.AddTransient<OrdinaryPacketHandler>();
        services.AddTransient<WhoAreYouPacketHandler>();
        services.AddTransient<HandshakePacketHandler>();
        
        return services;
    }
}