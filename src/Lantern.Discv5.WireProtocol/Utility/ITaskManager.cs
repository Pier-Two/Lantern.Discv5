using Microsoft.Extensions.Logging;

namespace Lantern.Discv5.WireProtocol.Utility;

public interface ITaskManager
{
    void Add(Func<CancellationToken, Task> taskFunc);

    Func<CancellationToken, Task> GetSafeTask(Func<CancellationToken, Task> taskFunc);

    void StartAll();
    
    Task StopAll();
}