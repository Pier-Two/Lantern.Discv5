using Lantern.Discv5.Enr;

namespace Lantern.Discv5.WireProtocol.Table;

public interface ITableManager
{ 
    Task StartTableManagerAsync(CancellationToken token = default);

    Task StopTableManagerAsync(CancellationToken token = default);
}