namespace Lantern.Discv5.WireProtocol.Connection;

public interface IConnectionManager
{
    void StartConnectionManagerAsync(CancellationToken token = default);

    Task StopConnectionManagerAsync(CancellationToken token = default);
}