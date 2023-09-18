using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lantern.Discv5.WireProtocol.Services;

public static class LoggerService
{
    public static IServiceCollection AddLoggerServices(
        this IServiceCollection services,
        ILoggerFactory loggerFactory)
    {
        services.AddSingleton(loggerFactory);
        services.AddSingleton(loggerFactory.CreateLogger<Discv5Protocol>());

        return services;
    }
}