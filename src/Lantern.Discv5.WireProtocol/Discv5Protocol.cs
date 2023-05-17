using Lantern.Discv5.Enr;
using Lantern.Discv5.WireProtocol.Connection;
using Lantern.Discv5.WireProtocol.Identity;
using Lantern.Discv5.WireProtocol.Packet;
using Lantern.Discv5.WireProtocol.Session;
using Lantern.Discv5.WireProtocol.Table;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lantern.Discv5.WireProtocol;

public class Discv5Protocol
{
    private readonly IConnectionService _connectionService;
    private readonly IIdentityManager _identityManager;
    private readonly ITableManager _tableManager;
    private readonly IRoutingTable _routingTable;
    private readonly ISessionManager _sessionManager;
    private readonly ILogger<Discv5Protocol> _logger;
    
    public Discv5Protocol(IServiceProvider serviceProvider)
    {
        _connectionService = serviceProvider.GetRequiredService<ConnectionService>();
        _identityManager = serviceProvider.GetRequiredService<IIdentityManager>();
        _tableManager = serviceProvider.GetRequiredService<ITableManager>();
        _routingTable = serviceProvider.GetRequiredService<IRoutingTable>();
        _sessionManager = serviceProvider.GetRequiredService<ISessionManager>();
        
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        _logger = loggerFactory.CreateLogger<Discv5Protocol>();
    }
    
    public EnrRecord SelfEnrRecord => _identityManager.Record;
    
    public int TotalEnrCount() => _routingTable.GetTotalEntriesCount();
    
    public int ActiveSessionCount() => _sessionManager.TotalSessionCount;

    public async Task StartDiscoveryAsync(CancellationToken token = default)
    {
        var connectionTask = _connectionService.StartConnectionServiceAsync(token);
        var tableTask = _tableManager.StartTableManagerAsync(token);
        
        await Task.WhenAll(connectionTask, tableTask).ConfigureAwait(false);
    }
    
    public async Task StopDiscoveryAsync(CancellationToken token = default)
    {
        await _connectionService.StopConnectionServiceAsync(token);
        await _tableManager.StopTableManagerAsync(token);
    }

    // Get connected peer count
    // Get an ENR from node id
    // Get all ENRs from the routing table
    // Perform a lookup for a node id
    // Ping a node
    // Send FINDNODE for a node id
    // Send TalkReq
    // Send TalkResp
    
}
