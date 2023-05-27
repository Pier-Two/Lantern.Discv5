using Lantern.Discv5.WireProtocol.Packet;
using Lantern.Discv5.WireProtocol.Table;
using Lantern.Discv5.WireProtocol.Utility;
using Microsoft.Extensions.Logging;

namespace Lantern.Discv5.WireProtocol.Discovery;

public class DiscoveryManager : IDiscoveryManager
{
    private readonly IRoutingTable _routingTable;
    private readonly IPacketManager _packetManager;
    private readonly ILookupManager _lookupManager;
    private readonly ILogger<DiscoveryManager> _logger;
    private readonly TableOptions _options;
    private readonly CancellationTokenSource _shutdownCts;
    private Task _initialiseTask;
    private Task _discoveryTask;
    
    public DiscoveryManager(IRoutingTable routingTable, IPacketManager packetManager, ILookupManager lookupManager, ILoggerFactory loggerFactory, TableOptions options)
    {
        _routingTable = routingTable;
        _packetManager = packetManager;
        _lookupManager = lookupManager;
        _logger = loggerFactory.CreateLogger<DiscoveryManager>();
        _options = options;
        _shutdownCts = new CancellationTokenSource();
        _initialiseTask = Task.CompletedTask;
        _discoveryTask = Task.CompletedTask;
    }
    
    public async Task StartDiscoveryManagerAsync(CancellationToken token = default)
    {
        _logger.LogInformation("Starting StartDiscoveryManagerAsync");
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(token, _shutdownCts.Token);

        try
        {
            _initialiseTask = InitialiseDiscoveryAsync();
            _discoveryTask = DiscoverAsync(linkedCts.Token);

            await Task.WhenAll(_initialiseTask, _discoveryTask).ConfigureAwait(false);
        }
        finally
        {
            linkedCts.Dispose();
        }
    }
    
    public async Task StopDiscoveryManagerAsync(CancellationToken token = default)
    {
        _logger.LogInformation("Stopping StartDiscoveryManagerAsync");
        _shutdownCts.Cancel();
        
        try
        {
            await Task.WhenAll(_initialiseTask, _discoveryTask).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested || _shutdownCts.IsCancellationRequested)
        {
            _logger.LogInformation("StartDiscoveryManagerAsync was canceled gracefully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred in StartDiscoveryManagerAsync");
        }
        
        _logger.LogInformation("StartServiceAsync completed");
    }
    
    private async Task InitialiseDiscoveryAsync()
    {
        if (_routingTable.GetTotalEntriesCount() == 0)
        {
            _logger.LogInformation("Initialising from bootstrap ENRs");
            var bootstrapEnrs = _routingTable.GetBootstrapEnrs();

            foreach (var bootstrapEnr in bootstrapEnrs)
            {
                try
                {
                    await _packetManager.SendPingPacket(bootstrapEnr);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error sending packet to bootstrap ENR: {BootstrapEnr}", bootstrapEnr);
                }
            }
        }
    }
    
    private async Task DiscoverAsync(CancellationToken token = default)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                await PerformDiscoveryAsync(); 
                await Task.Delay(_options.LookupIntervalMilliseconds, token)
                    .ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested || _shutdownCts.IsCancellationRequested)
        {
            _logger.LogInformation("StartDiscoveryManagerAsync was canceled gracefully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred in StartDiscoveryManagerAsync");
        }
        
        _logger.LogInformation("StartServiceAsync completed");
    }
    
    private async Task PerformDiscoveryAsync()
    {
        if (_routingTable.GetTotalActiveNodesCount() > 0)
        {
            _logger.LogInformation("Performing discovery...");
            var targetNodeId = RandomUtility.GenerateNodeId(PacketConstants.NodeIdSize);
            await _lookupManager.PerformLookup(targetNodeId);
        }
    }
}