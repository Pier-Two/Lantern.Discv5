namespace Lantern.Discv5.WireProtocol.Discovery;

public interface IDiscoveryManager
{
    Task StartDiscoveryManagerAsync(CancellationToken token = default);
    
    Task StopDiscoveryManagerAsync(CancellationToken token = default);
}