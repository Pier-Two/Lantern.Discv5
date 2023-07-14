using System.Net;
using Microsoft.Extensions.Logging;

namespace Lantern.Discv5.WireProtocol.Session;

public class SessionManager : ISessionManager
{
    private readonly IAesCrypto _aesCrypto;
    private readonly ISessionCrypto _sessionCrypto;
    private readonly ISessionKeys _sessionKeys;
    private readonly ILoggerFactory _loggerFactory;
    private readonly LruCache<SessionCacheKey, ISessionMain> _sessions;

    public SessionManager(SessionOptions options, IAesCrypto aesCrypto, ISessionCrypto sessionCrypto, ILoggerFactory loggerFactory)
    {
        _sessionKeys = options.SessionKeys;
        _aesCrypto = aesCrypto;
        _sessionCrypto = sessionCrypto;
        _loggerFactory = loggerFactory;
        _sessions = new LruCache<SessionCacheKey, ISessionMain>(options.CacheSize);
    }
    
    public int TotalSessionCount  => _sessions.Count;

    public ISessionMain CreateSession(SessionType sessionType, byte[] nodeId, IPEndPoint endPoint)
    {
        var key = new SessionCacheKey(nodeId, endPoint);
        var session = _sessions.Get(key);

        if (session != null) 
            return session;
        
        var newSession = CreateSession(sessionType);
        _sessions.Add(key, newSession);
        session = newSession;

        return session;
    }
    
    public ISessionMain? GetSession(byte[] nodeId, IPEndPoint endPoint)
    {
        var key = new SessionCacheKey(nodeId, endPoint);

        return _sessions.Get(key);
    }
    
    private ISessionMain CreateSession(SessionType sessionType)
    {
        var newSessionKeys = new SessionKeys(_sessionKeys.PrivateKey);
        var cryptoSession = new SessionMain(newSessionKeys, _aesCrypto, _sessionCrypto, _loggerFactory, sessionType);
        
        return cryptoSession;
    }
}