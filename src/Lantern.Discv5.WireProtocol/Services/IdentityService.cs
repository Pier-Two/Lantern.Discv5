using Lantern.Discv5.Enr;
using Lantern.Discv5.WireProtocol.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace Lantern.Discv5.WireProtocol.Services;

public static class IdentityService
{
    public static IServiceCollection AddIdentityServices(
        this IServiceCollection services,
        IEnrEntryRegistry enrEntryRegistry,
        IEnr enr)
    {
        services.AddSingleton(enrEntryRegistry);
        services.AddSingleton(enr);
        services.AddSingleton<IEnrFactory, EnrFactory>();
        services.AddSingleton<IIdentityManager, IdentityManager>();

        return services;
    }
}