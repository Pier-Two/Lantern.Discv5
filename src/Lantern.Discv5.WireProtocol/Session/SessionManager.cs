using System.Collections.Concurrent;
using System.Net;
using Lantern.Discv5.WireProtocol.Utility;
using Microsoft.Extensions.Caching.Memory;

namespace Lantern.Discv5.WireProtocol.Session;

public class SessionManager : ISessionManager
{
    private readonly IAesUtility _aesUtility;
    private readonly ISessionCrypto _sessionCrypto;
    private readonly ISessionKeys _sessionKeys;
    private readonly IMemoryCache _sessions;
    private readonly ConcurrentDictionary<byte[], byte[]> _cachedHandshakeInteractions;
    
    public SessionManager(SessionOptions options, IAesUtility aesUtility, ISessionCrypto sessionCrypto)
    {
        _sessionKeys = options.SessionKeys;
        _aesUtility = aesUtility;
        _sessionCrypto = sessionCrypto;

        // Configure the MemoryCache with the desired size and eviction policy
        var cacheOptions = new MemoryCacheOptions
        {
            SizeLimit = options.CacheSize,
            CompactionPercentage = 0.1
        };
        _sessions = new MemoryCache(cacheOptions);
        
        _cachedHandshakeInteractions = new ConcurrentDictionary<byte[], byte[]>(ByteArrayEqualityComparer.Instance);
    }
    
    public void SaveHandshakeInteraction(byte[] packetNonce, byte[] destNodeId)
    { 
        _cachedHandshakeInteractions.TryAdd(packetNonce, destNodeId);
    }
    
    public byte[]? GetHandshakeInteraction(byte[] packetNonce)
    {
        return _cachedHandshakeInteractions.TryRemove(packetNonce, out var destNodeId) ? destNodeId : null;
    }
    
    public SessionMain? CreateSession(SessionType sessionType, byte[] nodeId, IPEndPoint endPoint)
    {
        var key = new SessionCacheKey(nodeId, endPoint);

        var session = _sessions.GetOrCreate(key, entry =>
        {
            entry.SetSize(1);
            entry.Priority = CacheItemPriority.Normal;
            entry.RegisterPostEvictionCallback((_, _, _, _) =>
            {
                // Handle session eviction if necessary
            });

            return CreateSession(sessionType);
        });

        return session;
    }
    
    public SessionMain? GetSession(byte[] nodeId, IPEndPoint endPoint)
    {
        var key = new SessionCacheKey(nodeId, endPoint);

        if (_sessions.TryGetValue(key, out var session))
        {
            return session as SessionMain;
        }
        return null;
    }
    
    private SessionMain CreateSession(SessionType sessionType)
    {
        var newSessionKeys = new SessionKeys(_sessionKeys.PrivateKey);
        var cryptoSession = new SessionMain(newSessionKeys, _aesUtility, _sessionCrypto, sessionType);
        return cryptoSession;
    }
}