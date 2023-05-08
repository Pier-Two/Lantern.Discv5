using Lantern.Discv5.Enr;
using Lantern.Discv5.Enr.IdentityScheme.V4;
using Lantern.Discv5.WireProtocol.Identity;

namespace Lantern.Discv5.WireProtocol.Table;

public class TableManager : ITableManager
{
    private readonly IIdentityManager _identityManager;
    private readonly RoutingTable _routingTable;
    private readonly TableOptions _tableOptions;

    public TableManager(IIdentityManager identityManager, TableOptions tableOptions)
    {
        _identityManager = identityManager;
        _routingTable = new RoutingTable(tableOptions, identityManager.Verifier.GetNodeIdFromRecord(identityManager.Record));
        _tableOptions = tableOptions;
    }

    public int RecordCount => _routingTable.GetTotalEntriesCount();
    
    public IEnumerable<EnrRecord> GetBootstrapEnrs()
    {
        return _tableOptions.BootstrapEnrs;
    }

    public void UpdateTable(NodeTableEntry entry)
    {
        _routingTable.Update(entry);
    }
    
    public void UpdateTable(EnrRecord enrRecord)
    {
        var nodeId = _identityManager.Verifier.GetNodeIdFromRecord(enrRecord);
        var newNodeEntry = new NodeTableEntry(enrRecord, _identityManager.Verifier);
        _routingTable.Update(newNodeEntry);
        _routingTable.MarkNodeAsLive(nodeId);
    }
    
    public void MarkNodeAsQueried(byte[] nodeId)
    {
        _routingTable.MarkNodeAsQueried(nodeId);
    }

    public void RefreshTable()
    {
        if (RecordCount > 0)
        {
            Console.WriteLine("Refreshing buckets...");
            _routingTable.RefreshBuckets();
        }
    }

    public NodeTableEntry? GetNodeEntry(byte[] nodeId)
    {
        var nodeEntry = _routingTable.GetNodeEntry(nodeId);

        if (nodeEntry == null)
        {
            var bootstrapEnrs = GetBootstrapEnrs();

            foreach (var bootstrapEnr in bootstrapEnrs)
            {
                var bootstrapNodeId = _identityManager.Verifier.GetNodeIdFromRecord(bootstrapEnr);

                if (nodeId.SequenceEqual(bootstrapNodeId))
                {
                    var bootstrapEntry = new NodeTableEntry(bootstrapEnr, _identityManager.Verifier);
                    return bootstrapEntry;
                }
            }
        }
        else
        {
            return nodeEntry;
        }
        
        return null;
    }

    public List<EnrRecord> GetEnrRecordsAtDistances(IEnumerable<int> distances)
    {
        var enrRecords = new List<EnrRecord>();

        foreach (var distance in distances)
        {
            if (distance == 0)
            {
                enrRecords.Add(_identityManager.Record);
            }
            else
            {
                var nodesAtDistance = _routingTable.GetNodesAtDistance(distance);

                foreach (var nodeAtDistance in nodesAtDistance)
                {
                    enrRecords.Add(nodeAtDistance.Record);
                }
            }
        }

        return enrRecords;
    }

    
    public IEnumerable<NodeTableEntry> GetInitialNodesForLookup(byte[] targetNodeId)
    {
        return _routingTable.GetInitialNodesForLookup(targetNodeId);
    }

    public NodeTableEntry GetNodeForFindNode(byte[] targetNodeId)
    {
        return _routingTable.GetNodeForFindNode(targetNodeId);
    }

    public int[] GetClosestNeighbours(byte[] targetNodeId)
    {
        var neighbours = _routingTable.GetInitialNodesForLookup(targetNodeId);
        var distances = new int[neighbours.Count];
        var identityVerifier = new IdentitySchemeV4Verifier();
        
        foreach (var neighbour in neighbours)
        {
            var neighbourNodeId = identityVerifier.GetNodeIdFromRecord(neighbour.Record);
            var selfNodeId = identityVerifier.GetNodeIdFromRecord(_identityManager.Record);
            var distance = TableUtility.Log2Distance(neighbourNodeId, selfNodeId);
            distances[neighbours.IndexOf(neighbour)] = distance;
        }
        
        return distances;
    }
}