using Microsoft.Extensions.Logging;

namespace Lantern.Discv5.WireProtocol.Table;

public class KBucket
{
    private readonly LinkedList<NodeTableEntry> _nodes;
    private readonly LinkedList<NodeTableEntry> _replacementCache;
    private readonly ILogger<KBucket> _logger;
    private readonly object _lock;
    private readonly int _replacementCacheSize;

    public KBucket(ILoggerFactory loggerFactory, int replacementCacheSize)
    {
        _nodes = new LinkedList<NodeTableEntry>();
        _replacementCache = new LinkedList<NodeTableEntry>();
        _logger = loggerFactory.CreateLogger<KBucket>();
        _lock = new object();
        _replacementCacheSize = replacementCacheSize;
    }

    public IEnumerable<NodeTableEntry> Nodes => _nodes;
    
    public IEnumerable<NodeTableEntry> ReplacementCache => _replacementCache;
    
    public NodeTableEntry? GetNodeFromReplacementCache(byte[] nodeId)
    {
        lock (_lock)
        {
            return _replacementCache.FirstOrDefault(node => node.Id.SequenceEqual(nodeId));
        }
    }

    public NodeTableEntry? GetNodeById(byte[] nodeId)
    {
        lock (_lock)
        {
            return _nodes.FirstOrDefault(n => n.Id.SequenceEqual(nodeId)) ?? _replacementCache.FirstOrDefault(n => n.Id.SequenceEqual(nodeId));
        }
    }
    
    public NodeTableEntry? GetLeastRecentlySeenNode()
    {
        lock (_lock)
        {
            return _nodes.First?.Value;
        }
    }
    
    public void ClearReplacementCache()
    {
        lock (_lock)
        {
            _replacementCache.Clear();
        }
    }
    
    public void Update(NodeTableEntry nodeEntry)
    {
        lock (_lock)
        {
            var existingNode = GetNodeById(nodeEntry.Id);

            if (existingNode != null)
            {
                UpdateExistingNode(nodeEntry, existingNode);
            }
            else if (_nodes.Count >= TableConstants.BucketSize)
            {
                CheckLeastRecentlySeenNode(nodeEntry);
            }
            else
            {
                AddNewNode(nodeEntry);
            }
        }
    }

    public void ReplaceDeadNode(NodeTableEntry deadNodeEntry)
    {
        lock (_lock)
        {
            if (_replacementCache.Count == 0 || _replacementCache.First == null)
                return;
            
            _logger.LogDebug("Replacing dead node {NodeId} with node {ReplacementNodeId}", Convert.ToHexString(deadNodeEntry.Id), Convert.ToHexString(_replacementCache.First.Value.Id));
        
            var replacementNode = _replacementCache.First.Value;
            _replacementCache.RemoveFirst();
        
            var deadNode = _nodes.FirstOrDefault(node => node.Id.SequenceEqual(deadNodeEntry.Id));
            
            if(deadNode != null)
            {
                _nodes.Remove(deadNode);
            }
        
            replacementNode.LastSeen = DateTime.UtcNow;
            _nodes.AddLast(replacementNode);
        }
    }
    
    public void AddToReplacementCache(NodeTableEntry nodeEntry)
    {
        if (_replacementCache.Count >= _replacementCacheSize)
        {
            _logger.LogDebug("Replacement cache full. Removed last from the bucket");
            _replacementCache.RemoveFirst();
        }

        _replacementCache.AddLast(nodeEntry);
        _logger.LogDebug("Added node {NodeId} to replacement cache. There are {Count} nodes in the cache",
            Convert.ToHexString(nodeEntry.Id), _replacementCache.Count);
    }

    private void UpdateExistingNode(NodeTableEntry nodeEntry, NodeTableEntry existingNode)
    {
        if (nodeEntry.Status is NodeStatus.Live)
        {
            _nodes.Remove(existingNode);

            existingNode.LastSeen = DateTime.UtcNow;
            _nodes.AddLast(existingNode);
        }
        else
        {
            ReplaceDeadNode(nodeEntry);
        }
    }
    
    private void AddNewNode(NodeTableEntry nodeEntry)
    {
        nodeEntry.LastSeen = DateTime.UtcNow;
        _nodes.AddLast(nodeEntry);
    }

    private void CheckLeastRecentlySeenNode(NodeTableEntry nodeEntry)
    {
        var leastRecentlySeenNode = GetLeastRecentlySeenNode();
    
        if(leastRecentlySeenNode == null)
            return;
    
        if (leastRecentlySeenNode.Status is NodeStatus.Live or NodeStatus.Pending)
        {
            AddToReplacementCache(nodeEntry);
        }
        else
        {
            ReplaceDeadNode(leastRecentlySeenNode);
            AddToReplacementCache(nodeEntry);
        }
    }
}