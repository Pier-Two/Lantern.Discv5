using Microsoft.Extensions.Logging;

namespace Lantern.Discv5.WireProtocol.Utility;

public class CancellationHandler : ICancellationHandler
{
    public async Task HandleCancellationAsync(Func<Task> func, int delay, CancellationToken cancellationToken, ILogger logger)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await func();
            try
            {
                await Task.Delay(delay, cancellationToken);
            }
            catch (TaskCanceledException)
            {
                logger.LogInformation("Task was cancelled");
            }
        }
    }
}