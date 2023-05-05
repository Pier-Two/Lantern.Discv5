using Lantern.Discv5.WireProtocol.Connection;
using Microsoft.Extensions.DependencyInjection;

namespace Lantern.Discv5.WireProtocol;

public class Discv5Protocol
{
    private readonly ConnectionService _connectionService;

    public Discv5Protocol(IServiceProvider serviceProvider)
    {
        _connectionService = serviceProvider.GetRequiredService<ConnectionService>();
    }

    public async Task StartDiscoveryAsync()
    {
        await _connectionService.StartAsync();
        await _connectionService.StopAsync();
    }
}
