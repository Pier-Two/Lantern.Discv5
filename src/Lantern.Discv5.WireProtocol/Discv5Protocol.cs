using Lantern.Discv5.Enr;
using Lantern.Discv5.WireProtocol.Connection;
using Lantern.Discv5.WireProtocol.Identity;
using Lantern.Discv5.WireProtocol.Message;
using Lantern.Discv5.WireProtocol.Packet;
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
    private readonly IRequestManager _requestManager;
    private readonly IPacketManager _packetManager;
    private readonly IRoutingTable _routingTable;
    private readonly ISessionManager _sessionManager;
    private readonly ILookupManager _lookupManager;
    private readonly ILogger<Discv5Protocol> _logger;
    
    public Discv5Protocol(IServiceProvider serviceProvider)
    {
        _connectionManager = serviceProvider.GetRequiredService<IConnectionManager>();
        _identityManager = serviceProvider.GetRequiredService<IIdentityManager>();
        _tableManager = serviceProvider.GetRequiredService<ITableManager>();
        _requestManager = serviceProvider.GetRequiredService<IRequestManager>();
        _packetManager = serviceProvider.GetRequiredService<IPacketManager>();
        _routingTable = serviceProvider.GetRequiredService<IRoutingTable>();
        _sessionManager = serviceProvider.GetRequiredService<ISessionManager>();
        _lookupManager = serviceProvider.GetRequiredService<ILookupManager>();
        _logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger<Discv5Protocol>();
    }
    
    public EnrRecord SelfEnrRecord => _identityManager.Record;
    
    public int NodesCount() => _routingTable.GetTotalEntriesCount();
    
    public int PeerCount() => _routingTable.GetTotalActiveNodesCount();
    
    public int ActiveSessionCount() => _sessionManager.TotalSessionCount;
    
    public NodeTableEntry? GetNodeFromId(byte[] nodeId) => _routingTable.GetNodeEntry(nodeId);

    public NodeTableEntry[] GetAllNodes() => _routingTable.GetAllNodeEntries();

    public void StartProtocolAsync()
    {
        _connectionManager.StartConnectionManagerAsync();
        _tableManager.StartTableManagerAsync();
        _requestManager.StartRequestManager();
    }
    
    public async Task StopProtocolAsync()
    {
        var stopConnectionManagerTask = _connectionManager.StopConnectionManagerAsync();
        var stopTableManagerTask = _tableManager.StopTableManagerAsync();
        var stopRequestManagerTask = _requestManager.StopRequestManagerAsync();
        
        await Task.WhenAll(stopConnectionManagerTask, stopTableManagerTask, stopRequestManagerTask).ConfigureAwait(false);
    }

    public async Task<List<NodeTableEntry>?> PerformLookupAsync(byte[] targetNodeId)
    {
        if (_routingTable.GetTotalActiveNodesCount() > 0)
        {
            _logger.LogInformation("Performing lookup...");
            var closestNodes = await _lookupManager.LookupAsync(targetNodeId);
            
            if (closestNodes != null)
            {
                return closestNodes;
            }
        }
        
        return null;
    }
    
    public async Task SendPingAsync(EnrRecord destinationRecord)
    {
        try
        {
            await _packetManager.SendPacket(destinationRecord, MessageType.Ping);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred in SendPingAsync. Cannot send PING to {Record}", destinationRecord);
        }
    }

    public async Task SendFindNodeAsync(EnrRecord destinationRecord, byte[] nodeId)
    {
        try
        {
            await _packetManager.SendPacket(destinationRecord, MessageType.FindNode, nodeId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred in SendFindNodeAsync. Cannot send FINDNODE to {Record}", destinationRecord);
        }
    }
    
    public async Task SendTalkReqAsync(EnrRecord destinationRecord, byte[] protocol, byte[] request)
    {
        try
        {
            await _packetManager.SendPacket(destinationRecord, MessageType.TalkReq, protocol, request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred in SendTalkReqAsync. Cannot send TALKREQ to {Record}", destinationRecord);
        }
    }
    
    public async Task SendTalkRespAsync(EnrRecord destinationRecord, byte[] response)
    {
        try
        {
            await _packetManager.SendPacket(destinationRecord, MessageType.TalkResp, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred in SendTalkRespAsync. Cannot send TALKRESP to {Record}", destinationRecord);
        }
    }
}
