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

    public async Task StartServiceAsync()
    {
        await _connectionService.StartAsync();
        await Task.Delay(2000);
        await _connectionService.StopAsync();
    }
}
