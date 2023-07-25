using Microsoft.Extensions.Logging;

namespace Lantern.Discv5.WireProtocol.Utility;

public interface ICancellationHandler
{
    Task HandleCancellationAsync(Func<Task> func, int delay, CancellationToken cancellationToken, ILogger logger);
}