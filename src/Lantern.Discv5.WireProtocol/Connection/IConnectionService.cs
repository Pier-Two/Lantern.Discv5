namespace Lantern.Discv5.WireProtocol.Connection;

public interface IConnectionService
{
    public Task StartAsync(CancellationToken cancellationToken = default);

    public Task StopAsync(CancellationToken cancellationToken = default);
}