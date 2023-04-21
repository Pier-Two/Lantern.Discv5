using Lantern.Discv5.WireProtocol.Packets.Types;
using Microsoft.Extensions.DependencyInjection;

namespace Lantern.Discv5.WireProtocol.Packets.Handlers;

public class PacketHandlerFactory : IPacketHandlerFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Dictionary<PacketType, Type> _handlerTypes;

    public PacketHandlerFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _handlerTypes = new Dictionary<PacketType, Type>
        {
            { PacketType.Ordinary, typeof(OrdinaryPacketHandler) },
            { PacketType.WhoAreYou, typeof(WhoAreYouPacketHandler) },
            { PacketType.Handshake, typeof(HandshakePacketHandler) },
        };
    }

    public IPacketHandler GetPacketHandler(PacketType packetType)
    {
        if (_handlerTypes.TryGetValue(packetType, out var handlerType))
        {
            return (IPacketHandler)ActivatorUtilities.CreateInstance(_serviceProvider, handlerType);
        }

        throw new InvalidOperationException($"No handler found for packet type {packetType}");
    }
}