using Microsoft.Extensions.Logging;

namespace Lantern.Discv5.WireProtocol.Utility;

public class TaskManager : ITaskManager
{
    private readonly List<Task> _tasks = new();
    private readonly List<Func<CancellationToken, Task>> _taskGenerators = new();
    private readonly ILogger<TaskManager> _logger;
    private readonly ICancellationTokenSourceWrapper _cts;

    public TaskManager(ICancellationTokenSourceWrapper cts, ILoggerFactory loggerFactory)
    {
        _cts = cts;
        _logger = loggerFactory.CreateLogger<TaskManager>();
    }

    public void Add(Func<CancellationToken, Task> taskFunc)
    {
        _taskGenerators.Add(taskFunc);
    }
    
    public Func<CancellationToken, Task> GetSafeTask(Func<CancellationToken, Task> taskFunc)
    {
        return async ct =>
        {
            try
            {
                await taskFunc(ct);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Task was canceled gracefully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred when executing task");
            }
        };
    }
    
    public void StartAll()
    {
        _tasks.AddRange(_taskGenerators.Select(tg => tg.Invoke(_cts.GetToken())));
    }

    public async Task StopAll()
    {
        _cts.Cancel();

        try
        {
            await Task.WhenAll(_tasks).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Tasks were canceled gracefully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred when stopping tasks");
        }
    }
}