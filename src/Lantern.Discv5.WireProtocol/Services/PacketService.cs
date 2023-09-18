using Lantern.Discv5.WireProtocol.Packet;
using Lantern.Discv5.WireProtocol.Packet.Handlers;
using Microsoft.Extensions.DependencyInjection;

namespace Lantern.Discv5.WireProtocol.Services;

public static class PacketService
{
    public static IServiceCollection AddPacketServices(this IServiceCollection services)
    {
        services.AddSingleton<IPacketManager, PacketManager>();
        services.AddSingleton<IPacketBuilder, PacketBuilder>();
        services.AddSingleton<IPacketProcessor, PacketProcessor>();
        services.AddSingleton<IPacketHandlerFactory, PacketHandlerFactory>();
        services.AddTransient<OrdinaryPacketHandler>();
        services.AddTransient<WhoAreYouPacketHandler>();
        services.AddTransient<HandshakePacketHandler>();

        return services;
    }
}