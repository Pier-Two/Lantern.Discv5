using System.Net;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Lantern.Discv5.WireProtocol.Session;

public class SessionManager : ISessionManager
{
    private readonly IAesUtility _aesUtility;
    private readonly ISessionCrypto _sessionCrypto;
    private readonly ISessionKeys _sessionKeys;
    private readonly IMemoryCache _sessions;
    private readonly ILoggerFactory _loggerFactory;
    private int _sessionCount;
    
    public SessionManager(SessionOptions options, IAesUtility aesUtility, ISessionCrypto sessionCrypto, ILoggerFactory loggerFactory)
    {
        _sessionKeys = options.SessionKeys;
        _aesUtility = aesUtility;
        _sessionCrypto = sessionCrypto;
        _loggerFactory = loggerFactory;
        
        var cacheOptions = new MemoryCacheOptions
        {
            SizeLimit = options.CacheSize,
            CompactionPercentage = 0.1
        };
        _sessions = new MemoryCache(cacheOptions);
    }
    
    public int TotalSessionCount => _sessionCount;
    
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
                Interlocked.Decrement(ref _sessionCount);
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
        var cryptoSession = new SessionMain(newSessionKeys, _aesUtility, _sessionCrypto, _loggerFactory, sessionType);

        Interlocked.Increment(ref _sessionCount);
        
        return cryptoSession;
    }
}