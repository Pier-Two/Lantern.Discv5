using Lantern.Discv5.WireProtocol.Table;
using Lantern.Discv5.WireProtocol.Utility;
using Microsoft.Extensions.DependencyInjection;

namespace Lantern.Discv5.WireProtocol.Services;

public static class UtilityService
{
    public static IServiceCollection AddUtilityServices(this IServiceCollection services)
    {
        services.AddSingleton<Discv5Protocol>();
        services.AddSingleton<IGracefulTaskRunner, GracefulTaskRunner>();
        services.AddTransient<ICancellationTokenSourceWrapper, CancellationTokenSourceWrapper>();
        services.AddSingleton<IRoutingTable, RoutingTable>();
        
        return services;
    }
}