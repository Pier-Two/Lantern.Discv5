using Lantern.Discv5.WireProtocol.Message;
using Microsoft.Extensions.DependencyInjection;

namespace Lantern.Discv5.WireProtocol.Services;

public static class MessageService
{
    public static IServiceCollection AddMessageServices(this IServiceCollection services)
    {
        services.AddSingleton<IMessageDecoder, MessageDecoder>();
        services.AddSingleton<IMessageRequester, MessageRequester>();
        services.AddSingleton<IMessageResponder, MessageResponder>();
        services.AddSingleton<IRequestManager, RequestManager>();
        
        return services;
    }
}