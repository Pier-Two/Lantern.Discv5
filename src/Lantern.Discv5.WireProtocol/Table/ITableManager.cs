namespace Lantern.Discv5.WireProtocol.Table;

public interface ITableManager
{ 
    public CancellationTokenSource ShutdownCts { get; }
    
    void StartTableManagerAsync();

    Task StopTableManagerAsync();
    
    Task InitialiseFromBootstrapAsync();
    
    Task RefreshBucketsAsync();
    
    Task PingNodeAsync();

    Task RefreshBucket();
}