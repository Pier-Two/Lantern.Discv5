using System.Collections.Concurrent;
using Lantern.Discv5.WireProtocol.Utility;
using Microsoft.Extensions.Logging;

namespace Lantern.Discv5.WireProtocol.Table;

public class KBucket
{
    private readonly int _maxSize;
    private readonly LinkedList<NodeTableEntry> _nodes;
    private readonly List<LinkedList<NodeTableEntry>> _replacementCaches;
    private readonly ConcurrentDictionary<byte[], LinkedListNode<NodeTableEntry>> _nodeLookup;
    private readonly TableOptions _options;
    private readonly ILogger<KBucket> _logger;

    public KBucket(ILoggerFactory loggerFactory, TableOptions options, int maxSize)
    {
        _maxSize = maxSize;
        _options = options;
        _nodes = new LinkedList<NodeTableEntry>();
        _replacementCaches = Enumerable.Range(0, TableConstants.NumberOfBuckets)
            .Select(_ => new LinkedList<NodeTableEntry>())
            .ToList();
        _nodeLookup = new ConcurrentDictionary<byte[], LinkedListNode<NodeTableEntry>>(ByteArrayEqualityComparer.Instance);
        _logger = loggerFactory.CreateLogger<KBucket>();
    }

    public IEnumerable<NodeTableEntry> Nodes => _nodes;

    public NodeTableEntry? GetNodeById(byte[] nodeId)
    {
        if (_nodeLookup.TryGetValue(nodeId, out var node))
        {
            return node.Value;
        }

        return null;
    }
    
    public void Update(NodeTableEntry nodeEntry, int bucketIndex)
    {
        if (_nodeLookup.TryGetValue(nodeEntry.Id, out var node))
        {
            UpdateExistingNode(nodeEntry, node, bucketIndex);
        }
        else if (_nodes.Count >= _maxSize)
        {
            CheckLeastRecentlySeenNode(nodeEntry, bucketIndex);
        }
        else
        {
            AddNewNode(nodeEntry);
        }
    }
    
    private void UpdateExistingNode(NodeTableEntry nodeEntry, LinkedListNode<NodeTableEntry> node, int bucketIndex)
    {
        if (nodeEntry.IsLive)
        {
            _nodes.Remove(node); // Remove the old node from the _nodes list
            var newNode = new LinkedListNode<NodeTableEntry>(node.Value); // Create a new LinkedListNode with the updated value
            _nodes.AddLast(newNode); // Add the new node to the _nodes list
            _nodeLookup[nodeEntry.Id] = newNode; // Update the _nodeLookup with the new node
        }
        else
        {
            ReplaceDeadNode(nodeEntry, bucketIndex);
        }
    }
    
    private void AddNewNode(NodeTableEntry nodeEntry)
    {
        var newNode = _nodes.AddLast(nodeEntry);
        _nodeLookup[nodeEntry.Id] = newNode;
    }

    private void AddToReplacementCache(NodeTableEntry nodeEntry, int bucketIndex)
    {
        var replacementCache = _replacementCaches[bucketIndex];

        // Check if the replacement cache has reached its maximum size before adding a node
        if (replacementCache.Count < _options.MaxReplacementCacheSize)
        {
            replacementCache.AddLast(nodeEntry);
        }
    }

    public void ReplaceDeadNode(NodeTableEntry nodeEntry, int bucketIndex)
    {
        var replacementCache = _replacementCaches[bucketIndex];

        if (replacementCache.Count <= 0)
            return;

        var replacementNode = replacementCache.First.Value;
        replacementCache.RemoveFirst();

        // Remove the dead node from the _nodes list and _nodeLookup
        _nodes.Remove(_nodeLookup[nodeEntry.Id]);
        _nodeLookup.TryRemove(nodeEntry.Id, out _);

        // Add the replacement node to _nodes list and _nodeLookup
        var newNode = _nodes.AddLast(replacementNode);
        _nodeLookup[replacementNode.Id] = newNode;
    }

    public NodeTableEntry GetLeastRecentlySeenNode()
    {
        return _nodes.First.Value;
    }

    public void RefreshLeastRecentlySeenNode()
    {
        // Move the least recently seen node to the end of the list to simulate a refresh.
        var node = _nodes.First;

        if (node == null)
            return;

        var leastRecentlySeenNode = node.Value;
        _nodes.RemoveFirst();
        _nodes.AddLast(leastRecentlySeenNode);
    }
    
    private void CheckLeastRecentlySeenNode(NodeTableEntry nodeEntry, int bucketIndex)
    {
        var leastRecentlySeenNode = GetLeastRecentlySeenNode();

        if (leastRecentlySeenNode.IsLive)
        {
            AddToReplacementCache(nodeEntry, bucketIndex);
        }
        else
        {
            ReplaceDeadNode(leastRecentlySeenNode, bucketIndex);

            // Since we've removed the dead node, there should be room for the new node now
            AddNewNode(nodeEntry);
        }
    }
}