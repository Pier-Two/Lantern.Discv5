using Lantern.Discv5.WireProtocol.Packet;
using Lantern.Discv5.WireProtocol.Table;
using Microsoft.Extensions.Logging;

namespace Lantern.Discv5.WireProtocol.Discovery;

public class DiscoveryProtocol : IDiscoveryProtocol
{
    private readonly IRoutingTable _routingTable;
    private readonly IPacketManager _packetManager;
    private readonly ILogger<DiscoveryProtocol> _logger;
    private readonly CancellationTokenSource _shutdownCts;
    private Task _initialiseTask;

    public DiscoveryProtocol(IRoutingTable routingTable, IPacketManager packetManager, ILoggerFactory loggerFactory)
    {
        _routingTable = routingTable;
        _packetManager = packetManager;
        _logger = loggerFactory.CreateLogger<DiscoveryProtocol>();
        _shutdownCts = new CancellationTokenSource();
        _initialiseTask = Task.CompletedTask;
    }
    
    public void StartInitialiseProtocolAsync(CancellationToken token = default)
    {
        _logger.LogInformation("Starting StartInitialiseProtocolAsync");
        _initialiseTask = InitialiseDiscoveryAsync();
    }
    
    public async Task StopInitialiseProtocolAsync(CancellationToken token = default)
    {
        _logger.LogInformation("Stopping StartInitialiseProtocolAsync");
        _shutdownCts.Cancel();
        
        try
        {
            await _initialiseTask.ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (_shutdownCts.IsCancellationRequested)
        {
            _logger.LogInformation("StartInitialiseProtocolAsync was canceled gracefully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred in StartInitialiseProtocolAsync");
        }
        
        _logger.LogInformation("StartInitialiseProtocolAsync completed");
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
}