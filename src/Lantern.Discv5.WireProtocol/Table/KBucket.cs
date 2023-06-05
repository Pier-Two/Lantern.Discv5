using System.Collections.Concurrent;
using Lantern.Discv5.WireProtocol.Utility;
using Microsoft.Extensions.Logging;

namespace Lantern.Discv5.WireProtocol.Table;

public class KBucket
{
    private readonly LinkedList<NodeTableEntry> _nodes;
    private readonly LinkedList<NodeTableEntry> _replacementCache;
    private readonly ConcurrentDictionary<byte[], LinkedListNode<NodeTableEntry>> _nodeLookup;
    private readonly ILogger<KBucket> _logger;

    public KBucket(ILoggerFactory loggerFactory)
    {
        _nodes = new LinkedList<NodeTableEntry>();
        _replacementCache = new LinkedList<NodeTableEntry>();
        _nodeLookup = new ConcurrentDictionary<byte[], LinkedListNode<NodeTableEntry>>(ByteArrayEqualityComparer.Instance);
        _logger = loggerFactory.CreateLogger<KBucket>();
    }

    public IEnumerable<NodeTableEntry> Nodes => _nodes;
    
    public NodeTableEntry? GetNodeFromReplacementCache(byte[] nodeId)
    {
        var nodeInReplacementCache = _replacementCache.FirstOrDefault(node => ByteArrayEqualityComparer.Instance.Equals(node.Id, nodeId));

        return nodeInReplacementCache;
    }

    public NodeTableEntry? GetNodeById(byte[] nodeId)
    {
        if (_nodeLookup.TryGetValue(nodeId, out var node))
        {
            return node.Value;
        }

        return null;
    }
    
    public void Update(NodeTableEntry nodeEntry)
    {
        if (_nodeLookup.TryGetValue(nodeEntry.Id, out var node))
        {
            UpdateExistingNode(nodeEntry, node);
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
    
    public void ReplaceDeadNode(NodeTableEntry nodeEntry)
    {
        if (_replacementCache.Count <= 0)
            return;

        var replacementNode = _replacementCache.First.Value;
        
        _replacementCache.RemoveFirst();
        _nodes.Remove(_nodeLookup[nodeEntry.Id]);
        _nodeLookup.TryRemove(nodeEntry.Id, out _);
        replacementNode.LastSeen = DateTime.UtcNow; 
        
        var newNode = _nodes.AddLast(replacementNode);
        _nodeLookup[replacementNode.Id] = newNode;
    }
    
    public void RefreshLeastRecentlySeenNode()
    {
        var node = _nodes.First;

        if (node == null)
            return;

        var leastRecentlySeenNode = node.Value;
        _nodes.RemoveFirst();
        
        leastRecentlySeenNode.LastSeen = DateTime.UtcNow;
        _nodes.AddLast(leastRecentlySeenNode);
    }
    
    private void UpdateExistingNode(NodeTableEntry nodeEntry, LinkedListNode<NodeTableEntry> node)
    {
        if (nodeEntry.IsLive)
        {
            _nodes.Remove(node); 
            
            var newNode = new LinkedListNode<NodeTableEntry>(node.Value); 
            newNode.Value.LastSeen = DateTime.UtcNow;
            _nodes.AddLast(newNode); 
            _nodeLookup[nodeEntry.Id] = newNode;
        }
        else
        {
            ReplaceDeadNode(nodeEntry);
        }
    }
    
    private void AddNewNode(NodeTableEntry nodeEntry)
    {
        nodeEntry.LastSeen = DateTime.UtcNow; 
        var newNode = _nodes.AddLast(nodeEntry);
        _nodeLookup[nodeEntry.Id] = newNode;
    }

    private void AddToReplacementCache(NodeTableEntry nodeEntry)
    {
        _replacementCache.AddLast(nodeEntry);
        _logger.LogDebug("Added node {NodeId} to replacement cache", Convert.ToHexString(nodeEntry.Id));
    }

    private NodeTableEntry GetLeastRecentlySeenNode()
    {
        return _nodes.First.Value;
    }

    private void CheckLeastRecentlySeenNode(NodeTableEntry nodeEntry)
    {
        var leastRecentlySeenNode = GetLeastRecentlySeenNode();

        if (leastRecentlySeenNode.IsLive)
        {
            AddToReplacementCache(nodeEntry);
        }
        else
        {
            ReplaceDeadNode(leastRecentlySeenNode);
            AddNewNode(nodeEntry);
        }
    }
}