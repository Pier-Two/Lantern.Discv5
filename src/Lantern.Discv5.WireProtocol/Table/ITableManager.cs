namespace Lantern.Discv5.WireProtocol.Table;

public interface ITableManager
{ 
    void StartTableManagerAsync();

    Task StopTableManagerAsync();
}