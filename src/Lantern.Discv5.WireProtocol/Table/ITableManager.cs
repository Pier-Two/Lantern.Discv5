namespace Lantern.Discv5.WireProtocol.Table;

public interface ITableManager
{
    Task InitAsync();

    Task StopTableManagerAsync();
    
    Task InitialiseFromBootstrapAsync();
    
    Task RefreshBucketsAsync(CancellationToken token);
    
    Task PingNodeAsync(CancellationToken token);

    Task RefreshBucket();
}