using Lantern.Discv5.WireProtocol.Utility;
using Microsoft.Extensions.Logging;

namespace Lantern.Discv5.WireProtocol.Table;

public class KBucket
{
    private readonly LinkedList<NodeTableEntry> _nodes;
    private readonly LinkedList<NodeTableEntry> _replacementCache;
    private readonly ILogger<KBucket> _logger;

    public KBucket(ILoggerFactory loggerFactory)
    {
        _nodes = new LinkedList<NodeTableEntry>();
        _replacementCache = new LinkedList<NodeTableEntry>();
        _logger = loggerFactory.CreateLogger<KBucket>();
    }

    public IEnumerable<NodeTableEntry> Nodes => _nodes;
    
    public IEnumerable<NodeTableEntry> ReplacementCache => _replacementCache;

    public NodeTableEntry? GetNodeFromReplacementCache(byte[] nodeId)
    {
        var nodeInReplacementCache = _replacementCache.FirstOrDefault(node => ByteArrayEqualityComparer.Instance.Equals(node.Id, nodeId));

        return nodeInReplacementCache;
    }

    public NodeTableEntry? GetNodeById(byte[] nodeId)
    {
        return _nodes.FirstOrDefault(n => ByteArrayEqualityComparer.Instance.Equals(n.Id, nodeId));
    }
    
    public void Update(NodeTableEntry nodeEntry)
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

    public void ReplaceDeadNode(NodeTableEntry deadNodeEntry)
    {
        if (_replacementCache.Count == 0 || _replacementCache.First == null)
            return;
        
        var replacementNode = _replacementCache.First.Value;
        _replacementCache.RemoveFirst();
        
        var deadNode = _nodes.FirstOrDefault(node => ByteArrayEqualityComparer.Instance.Equals(node.Id, deadNodeEntry.Id));
        if(deadNode != null)
        {
            _nodes.Remove(deadNode);
        }
        
        replacementNode.LastSeen = DateTime.UtcNow;
        _nodes.AddLast(replacementNode);
    }

    public NodeTableEntry? GetLeastRecentlySeenNode()
    {
        return _nodes.First?.Value;
    }
    
    public void AddToReplacementCache(NodeTableEntry nodeEntry)
    {
        _replacementCache.AddLast(nodeEntry);
        _logger.LogDebug("Added node {NodeId} to replacement cache", Convert.ToHexString(nodeEntry.Id));
    }

    private void UpdateExistingNode(NodeTableEntry nodeEntry, NodeTableEntry existingNode)
    {
        if (nodeEntry.IsLive)
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
    
        if (leastRecentlySeenNode.IsLive)
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