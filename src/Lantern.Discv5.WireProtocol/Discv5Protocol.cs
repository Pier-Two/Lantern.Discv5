using Lantern.Discv5.Enr;
using Lantern.Discv5.WireProtocol.Connection;
using Lantern.Discv5.WireProtocol.Discovery;
using Lantern.Discv5.WireProtocol.Identity;
using Lantern.Discv5.WireProtocol.Session;
using Lantern.Discv5.WireProtocol.Table;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lantern.Discv5.WireProtocol;

public class Discv5Protocol
{
    private readonly IConnectionManager _connectionManager;
    private readonly IIdentityManager _identityManager;
    private readonly ITableManager _tableManager;
    private readonly IDiscoveryManager _discoveryManager;
    private readonly IRoutingTable _routingTable;
    private readonly ISessionManager _sessionManager;
    private readonly ILookupManager _lookupManager;
    private readonly ILogger<Discv5Protocol> _logger;
    
    public Discv5Protocol(IServiceProvider serviceProvider)
    {
        _connectionManager = serviceProvider.GetRequiredService<IConnectionManager>();
        _identityManager = serviceProvider.GetRequiredService<IIdentityManager>();
        _tableManager = serviceProvider.GetRequiredService<ITableManager>();
        _discoveryManager = serviceProvider.GetRequiredService<IDiscoveryManager>();
        _routingTable = serviceProvider.GetRequiredService<IRoutingTable>();
        _sessionManager = serviceProvider.GetRequiredService<ISessionManager>();
        _lookupManager = serviceProvider.GetRequiredService<ILookupManager>();
        _logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger<Discv5Protocol>();
    }
    
    public EnrRecord SelfEnrRecord => _identityManager.Record;
    
    public int TotalEnrCount() => _routingTable.GetTotalEntriesCount();
    
    public int ActiveSessionCount() => _sessionManager.TotalSessionCount;

    public async Task StartProtocolAsync(CancellationToken token = default)
    {
        var connectionTask = _connectionManager.StartConnectionManagerAsync(token);
        var discoveryTask = _discoveryManager.StartDiscoveryManagerAsync(token);
        var tableTask = _tableManager.StartTableManagerAsync(token);
        
        await Task.WhenAll(connectionTask, discoveryTask, tableTask).ConfigureAwait(false);
    }
    
    public async Task StopProtocolAsync(CancellationToken token = default)
    {
        await _connectionManager.StopConnectionManagerAsync(token);
        await _discoveryManager.StopDiscoveryManagerAsync(token);
        await _tableManager.StopTableManagerAsync(token);
    }

    public async Task PerformLookup(byte[] targetNodeId)
    {
        _logger.LogInformation("Performing lookup...");
        var closestNodes = await _lookupManager.PerformLookup(targetNodeId);
        _logger.LogInformation("Lookup completed. Closest nodes found:");
        
        foreach (var node in closestNodes)
        {
            _logger.LogInformation("Node ID: {NodeId}", Convert.ToHexString(node.Id));
        }
    }

    // TODO: Add methods for:
    // Get connected peer count
    // Get an ENR from node id
    // Get all ENRs from the routing table
    // Perform a lookup for a node id
    // Ping a node
    // Send FINDNODE for a node id
    // Send TalkReq
    // Send TalkResp
}
