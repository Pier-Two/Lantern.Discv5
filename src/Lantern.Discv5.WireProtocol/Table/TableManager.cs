using Lantern.Discv5.WireProtocol.Packet;
using Microsoft.Extensions.Logging;

namespace Lantern.Discv5.WireProtocol.Table;

public class TableManager : ITableManager
{
    private readonly IPacketService _packetService;
    private readonly IRoutingTable _routingTable;
    private readonly TableOptions _options;
    private readonly ILogger<TableManager> _logger;
    private readonly CancellationTokenSource _shutdownCts;
    private Task? _discoveryTask;
    private Task? _refreshTask;
    private Task? _pingTask;

    public TableManager(IPacketService packetService, IRoutingTable routingTable, TableOptions options, ILoggerFactory loggerFactory)
    {
        _packetService = packetService;
        _routingTable = routingTable;
        _options = options;
        _logger = loggerFactory.CreateLogger<TableManager>();
        _shutdownCts = new CancellationTokenSource();
    }
    
    // Maybe move Lookup method here?
    public async Task StartTableManagerAsync(CancellationToken token = default)
    {
        _logger.LogInformation("Starting TableManagerAsync");
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(token, _shutdownCts.Token);

        try
        {
            _discoveryTask = StartDiscoveryServiceAsync(linkedCts.Token);
            _refreshTask = RefreshBucketsAsync(linkedCts.Token);
            _pingTask = PingNodeAsync(linkedCts.Token);
            
            await Task.WhenAll(_discoveryTask, _refreshTask, _pingTask).ConfigureAwait(false);
        }
        finally
        {
            linkedCts.Dispose();
        }
    }
    
    public async Task StopTableManagerAsync(CancellationToken token = default)
    {
        _logger.LogInformation("Stopping TableManagerAsync");
        _shutdownCts.Cancel();
        
        try
        {
            if (_discoveryTask != null && _refreshTask != null && _pingTask != null)
            {
                await Task.WhenAll(_discoveryTask, _refreshTask, _pingTask).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested || _shutdownCts.IsCancellationRequested)
        {
            _logger.LogInformation("TableManagerAsync was canceled gracefully");
        }
    }

    private async Task StartDiscoveryServiceAsync(CancellationToken token = default)
    {
        _logger.LogInformation("Starting StartDiscoveryServiceAsync");
        
        try
        {
            while (!token.IsCancellationRequested)
            {
                await _packetService.InitialiseDiscoveryAsync().ConfigureAwait(false);
                await Task.Delay(_options.LookupIntervalMilliseconds, token)
                    .ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested || _shutdownCts.IsCancellationRequested)
        {
            _logger.LogInformation("StartDiscoveryServiceAsync was canceled gracefully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred in StartDiscoveryServiceAsync");
        }
        
        _logger.LogInformation("StartServiceAsync completed");
    }

    private async Task RefreshBucketsAsync(CancellationToken token = default)
    {
        _logger.LogInformation("Starting RefreshBucketsAsync");
        
        try
        {
            while (!token.IsCancellationRequested)
            {
                _routingTable.RefreshBuckets();
                await Task.Delay(_options.RefreshIntervalMilliseconds, token)
                    .ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested || _shutdownCts.IsCancellationRequested)
        {
            _logger.LogInformation("RefreshBucketsAsync was canceled gracefully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred in RefreshBucketsAsync");
        }
        
        _logger.LogInformation("RefreshBucketsAsync completed");
    }
    
    private async Task PingNodeAsync(CancellationToken token = default)
    {
        _logger.LogInformation("Starting PingNodeAsync");
        
        try
        {
            while (!token.IsCancellationRequested)
            {
                await _packetService.PingNodeAsync().ConfigureAwait(false);
                await Task.Delay(_options.PingIntervalMilliseconds, token)
                    .ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested || _shutdownCts.IsCancellationRequested)
        {
            _logger.LogInformation("PingNodeAsync was canceled gracefully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred in PingNodeAsync");
        }
        
        _logger.LogInformation("PingNodeAsync completed");
    }
}