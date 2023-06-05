namespace Lantern.Discv5.WireProtocol.Connection;

public interface IConnectionManager
{
    void StartConnectionManagerAsync();

    Task StopConnectionManagerAsync();
}