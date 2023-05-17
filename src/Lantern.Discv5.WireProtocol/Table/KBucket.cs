using System.Collections.Concurrent;
using Lantern.Discv5.WireProtocol.Utility;

namespace Lantern.Discv5.WireProtocol.Table;

public class KBucket
{
    private readonly int _maxSize;
    private readonly LinkedList<NodeTableEntry> _nodes;
    private readonly List<LinkedList<NodeTableEntry>> _replacementCaches;
    private readonly ConcurrentDictionary<byte[], LinkedListNode<NodeTableEntry>> _nodeLookup;
    private readonly TableOptions _options;

    public KBucket(TableOptions options, int maxSize)
    {
        _maxSize = maxSize;
        _options = options;
        _nodes = new LinkedList<NodeTableEntry>();
        _replacementCaches = Enumerable.Range(0, TableConstants.NumberOfBuckets)
            .Select(_ => new LinkedList<NodeTableEntry>())
            .ToList();
        _nodeLookup = new ConcurrentDictionary<byte[], LinkedListNode<NodeTableEntry>>(ByteArrayEqualityComparer.Instance);
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
        var replacementCache = _replacementCaches[bucketIndex];

        while (true)
        {
            if (_nodeLookup.TryGetValue(nodeEntry.Id, out var node))
            {
                if (nodeEntry.IsLive)
                {
                    node.Value.LivenessCounter++; // Increment the LivenessCounter
                    _nodes.Remove(node); // Remove the old node from the _nodes list
                    var newNode =
                        new LinkedListNode<NodeTableEntry>(node
                            .Value); // Create a new LinkedListNode with the updated value
                    _nodes.AddLast(newNode); // Add the new node to the _nodes list
                    _nodeLookup[nodeEntry.Id] = newNode; // Update the _nodeLookup with the new node
                }
                else
                {
                    if (node.List == _nodes)
                    {
                        _nodes.Remove(node);
                    }
                    
                    _nodeLookup.TryRemove(nodeEntry.Id, out _);

                    if (replacementCache.Count <= 0)
                        return;

                    var replacementNode = replacementCache.First.Value;
                    replacementCache.RemoveFirst();
                    nodeEntry = replacementNode;

                    // Add the replacement node to _nodeLookup
                    var newNode = _nodes.AddLast(nodeEntry);
                    _nodeLookup[nodeEntry.Id] = newNode;

                    continue;
                }
            }
            else
            {
                if (_nodes.Count >= _maxSize)
                {
                    // Check if the replacement cache has reached its maximum size before adding a node
                    if (replacementCache.Count < _options.MaxReplacementCacheSize)
                    {
                        replacementCache.AddLast(nodeEntry);
                    }
                }
                else
                {
                    var newNode = _nodes.AddLast(nodeEntry);
                    _nodeLookup[nodeEntry.Id] = newNode;
                }
            }

            break;
        }
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
}