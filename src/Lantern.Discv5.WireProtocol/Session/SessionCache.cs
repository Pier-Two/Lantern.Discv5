using System.Collections;
using System.Collections.Concurrent;
using System.Net;

namespace Lantern.Discv5.WireProtocol.Session;

public class SessionCache
{
    private readonly int _maxSize;
    private readonly ISessionManager _sessionManager;
    private readonly ConcurrentDictionary<(byte[], IPEndPoint), LinkedListNode<CacheItem>> _sessions;
    private readonly ConcurrentDictionary<byte[], byte[]> _cachedHandshakeInteractions;
    private readonly LinkedList<CacheItem> _lruList;

    public SessionCache(int maxSize, ISessionManager sessionManager)
    {
        _maxSize = maxSize;
        _sessionManager = sessionManager;
        _sessions = new ConcurrentDictionary<ValueTuple<byte[], IPEndPoint>, LinkedListNode<CacheItem>>(ByteArrayEndPointComparer.Instance);
        _cachedHandshakeInteractions = new ConcurrentDictionary<byte[], byte[]>(ByteArrayEqualityComparer.Instance);
        _lruList = new LinkedList<CacheItem>();
    }

    public bool AddHandshakeInteraction(byte[] packetNonce, byte[] destNodeId)
    {
        return _cachedHandshakeInteractions.TryAdd(packetNonce, destNodeId);
    }
    
    public byte[]? GetHandshakeInteraction(byte[] packetNonce)
    {
        if (_cachedHandshakeInteractions.TryRemove(packetNonce, out var destNodeId))
        {
            return destNodeId;
        }

        return null;
    }
    
    public CryptoSession CreateSession(SessionType sessionType, byte[] nodeId, IPEndPoint endPoint, byte[] challengeData)
    {
        var key = ValueTuple.Create(nodeId, endPoint);
        var newNode = _sessions.GetOrAdd(key, _ =>
        {
            var newSession = _sessionManager.CreateSession(sessionType, challengeData);
            var node = new LinkedListNode<CacheItem>(new CacheItem(key, newSession));

            lock (_lruList)
            {
                _lruList.AddFirst(node);
                EnsureCacheSize();
            }

            return node;
        });

        return newNode.Value.Session;
    }
    
    public CryptoSession? GetSession(byte[] nodeId, IPEndPoint endPoint)
    {
        var key = (nodeId, endPoint);
        if (_sessions.TryGetValue(key, out var node))
        {
            lock (_lruList)
            {
                if (node.Previous != null)
                {
                    _lruList.Remove(node);
                    _lruList.AddFirst(node);
                }
            }
            return node.Value.Session;
        }

        return null;
    }
    
    private void EnsureCacheSize()
    {
        while (_lruList.Count > _maxSize)
        {
            var lastNode = _lruList.Last;
            if (lastNode != null)
            {
                _sessions.TryRemove(lastNode.Value.Key, out _);
                lock (_lruList)
                {
                    _lruList.RemoveLast();
                }
            }
        }
    }

    private class CacheItem
    {
        public (byte[], IPEndPoint) Key { get; }
        public CryptoSession Session { get; }

        public CacheItem((byte[], IPEndPoint) key, CryptoSession session)
        {
            Key = key;
            Session = session;
        }
    }
    
    private static class ByteArrayEqualityComparer
    {
        public static readonly IEqualityComparer<byte[]> Instance = new ByteArrayEqualityComparerImplementation();

        private class ByteArrayEqualityComparerImplementation : IEqualityComparer<byte[]>
        {
            public bool Equals(byte[]? x, byte[]? y)
            {
                return StructuralComparisons.StructuralEqualityComparer.Equals(x, y);
            }

            public int GetHashCode(byte[] obj)
            {
                return StructuralComparisons.StructuralEqualityComparer.GetHashCode(obj);
            }
        }
    }
    
    private class ByteArrayEndPointComparer : IEqualityComparer<ValueTuple<byte[], IPEndPoint>>
    {
        public static readonly ByteArrayEndPointComparer Instance = new();

        private ByteArrayEndPointComparer() { }

        public bool Equals(ValueTuple<byte[], IPEndPoint> x, ValueTuple<byte[], IPEndPoint> y)
        {
            return x.Item1.SequenceEqual(y.Item1) && x.Item2.Equals(y.Item2);
        }

        public int GetHashCode(ValueTuple<byte[], IPEndPoint> obj)
        {
            var hash = 17;
            
            unchecked
            {
                hash = obj.Item1.Aggregate(hash, (current, b) => current * 31 + b.GetHashCode());
            }

            hash = hash * 31 + obj.Item2.GetHashCode();
            return hash;
        }
    }
}