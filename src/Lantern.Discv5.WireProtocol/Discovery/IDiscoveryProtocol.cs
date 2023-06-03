namespace Lantern.Discv5.WireProtocol.Discovery;

public interface IDiscoveryProtocol
{
    void StartInitialiseProtocolAsync(CancellationToken token = default);
    
    Task StopInitialiseProtocolAsync(CancellationToken token = default);
}