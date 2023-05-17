namespace Lantern.Discv5.WireProtocol.Connection;

public interface IConnectionService
{
    Task StartConnectionServiceAsync(CancellationToken cancellationToken = default);

    Task StopConnectionServiceAsync(CancellationToken cancellationToken = default);
}