using Lantern.Discv5.WireProtocol.Message;
using Lantern.Discv5.WireProtocol.Packet;
using Lantern.Discv5.WireProtocol.Table;
using Lantern.Discv5.WireProtocol.Utility;
using Microsoft.Extensions.Logging;

namespace Lantern.Discv5.WireProtocol.Discovery;

public class DiscoveryManager : IDiscoveryManager
{
    private readonly IRoutingTable _routingTable;
    private readonly IPacketManager _packetManager;
    private readonly ILogger<DiscoveryManager> _logger;
    private readonly TableOptions _options;
    private readonly CancellationTokenSource _shutdownCts;
    private Task? _discoveryTask;
    
    public DiscoveryManager(IRoutingTable routingTable, IPacketManager packetManager, ILoggerFactory loggerFactory, TableOptions options)
    {
        _routingTable = routingTable;
        _packetManager = packetManager;
        _logger = loggerFactory.CreateLogger<DiscoveryManager>();
        _options = options;
        _shutdownCts = new CancellationTokenSource();
    }
    
    public async Task StartDiscoveryManagerAsync(CancellationToken token = default)
    {
        _logger.LogInformation("Starting StartDiscoveryManagerAsync");
        await InitialiseDiscoveryAsync().ConfigureAwait(false);
        
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
    
    public async Task StopDiscoveryManagerAsync(CancellationToken token = default)
    {
        _logger.LogInformation("Stopping StartDiscoveryManagerAsync");
        _shutdownCts.Cancel();
        
        try
        {
            if (_discoveryTask != null)
            {
                await _discoveryTask.ConfigureAwait(false);
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
                    await _packetManager.SendPacket(MessageType.Ping, bootstrapEnr);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error sending packet to bootstrap ENR: {BootstrapEnr}", bootstrapEnr);
                }
            }
        }
    }
    
    private async Task PerformDiscoveryAsync()
    {
        _logger.LogInformation("Performing discovery...");
        var targetNodeId = RandomUtility.GenerateNodeId(PacketConstants.NodeIdSize);
        var initialNodesForLookup = _routingTable.GetInitialNodesForLookup(targetNodeId);
        
        // Establish sessions with initial nodes
        foreach (var nodeEntry in initialNodesForLookup)
        {
            if (!nodeEntry.IsQueried)
            {
                await _packetManager.SendPacket(MessageType.FindNode, nodeEntry.Record);
            }
        }
    }
}