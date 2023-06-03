namespace Lantern.Discv5.WireProtocol.Connection;

public interface IConnectionManager
{
    void StartConnectionManagerAsync(CancellationToken cancellationToken = default);

    Task StopConnectionManagerAsync(CancellationToken cancellationToken = default);
}