using Lantern.Discv5.WireProtocol.Session;
using Microsoft.Extensions.DependencyInjection;

namespace Lantern.Discv5.WireProtocol.Services;

public static class SessionService
{
    public static IServiceCollection AddSessionServices(this IServiceCollection services)
    {
        services.AddSingleton<IAesCrypto, AesCrypto>();
        services.AddSingleton<ISessionCrypto, SessionCrypto>();
        services.AddSingleton<ISessionManager, SessionManager>();
        
        return services;
    }
}