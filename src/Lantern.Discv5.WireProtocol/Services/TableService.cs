using Lantern.Discv5.WireProtocol.Table;
using Microsoft.Extensions.DependencyInjection;

namespace Lantern.Discv5.WireProtocol.Services;

public static class TableService
{
    public static IServiceCollection AddTableServices(this IServiceCollection services)
    {
        services.AddSingleton<IRoutingTable, RoutingTable>();
        services.AddSingleton<ITableManager, TableManager>();
        services.AddSingleton<ILookupManager, LookupManager>();
        
        return services;
    }
}