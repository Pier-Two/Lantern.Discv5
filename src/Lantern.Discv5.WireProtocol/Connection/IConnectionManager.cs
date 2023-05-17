namespace Lantern.Discv5.WireProtocol.Connection;

public interface IConnectionManager
{
    Task StartConnectionManagerAsync(CancellationToken cancellationToken = default);

    Task StopConnectionManagerAsync(CancellationToken cancellationToken = default);
}