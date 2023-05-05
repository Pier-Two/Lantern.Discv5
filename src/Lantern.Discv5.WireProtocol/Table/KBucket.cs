using System.Collections.Concurrent;

namespace Lantern.Discv5.WireProtocol.Table;

public class KBucket
{
    private readonly int _maxSize;
    private readonly int _maxReplacementCacheSize; // Added max size for replacement cache
    private readonly LinkedList<NodeTableEntry> _nodes;
    private readonly LinkedList<NodeTableEntry> _replacementCache;
    private readonly ConcurrentDictionary<byte[], LinkedListNode<NodeTableEntry>> _nodeLookup;

    public KBucket(int maxSize, int maxReplacementCacheSize) // Added maxReplacementCacheSize parameter
    {
        _maxSize = maxSize;
        _maxReplacementCacheSize = maxReplacementCacheSize;
        _nodes = new LinkedList<NodeTableEntry>();
        _replacementCache = new LinkedList<NodeTableEntry>();
        _nodeLookup = new ConcurrentDictionary<byte[], LinkedListNode<NodeTableEntry>>(new ByteArrayComparer());
    }

    public IEnumerable<NodeTableEntry> Nodes => _nodes;
    
    public NodeTableEntry GetNodeById(byte[] nodeId)
    {
        if (_nodeLookup.TryGetValue(nodeId, out var node))
        {
            return node.Value;
        }

        return null;
    }

    public void Update(NodeTableEntry nodeEntry)
    {
        while (true)
        {
            if (_nodeLookup.TryGetValue(nodeEntry.Id, out var node))
            {
                if (nodeEntry.IsLive)
                {
                    node.Value.LivenessCounter++; // Increment the LivenessCounter
                    _nodes.Remove(node);
                    _nodes.AddLast(node);
                }
                else
                {
                    _nodes.Remove(node);
                    _nodeLookup.TryRemove(nodeEntry.Id, out _);

                    if (_replacementCache.Count <= 0) return;

                    var replacementNode = _replacementCache.First.Value;
                    _replacementCache.RemoveFirst();
                    nodeEntry = replacementNode;
                    continue;
                }
            }
            else
            {
                if (_nodes.Count >= _maxSize)
                {
                    // Check if the replacement cache has reached its maximum size before adding a node
                    if (_replacementCache.Count < _maxReplacementCacheSize)
                    {
                        _replacementCache.AddLast(nodeEntry);
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
        var leastRecentlySeenNode = _nodes.First.Value;
        _nodes.RemoveFirst();
        _nodes.AddLast(leastRecentlySeenNode);
    }
}

public class ByteArrayComparer : IEqualityComparer<byte[]>
{
    public bool Equals(byte[] x, byte[] y)
    {
        return x.SequenceEqual(y);
    }

    public int GetHashCode(byte[] obj)
    {
        return obj.Aggregate(17, (current, b) => current * 31 + b.GetHashCode());
    }
}