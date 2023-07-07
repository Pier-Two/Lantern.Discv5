using System.Net;
using Microsoft.Extensions.Logging;

namespace Lantern.Discv5.WireProtocol.Session;

public class SessionManager : ISessionManager
{
    private readonly IAesUtility _aesUtility;
    private readonly ISessionCrypto _sessionCrypto;
    private readonly ISessionKeys _sessionKeys;
    private readonly ILoggerFactory _loggerFactory;
    private readonly LruCache<SessionCacheKey, SessionMain> _sessions;

    public SessionManager(SessionOptions options, IAesUtility aesUtility, ISessionCrypto sessionCrypto, ILoggerFactory loggerFactory)
    {
        _sessionKeys = options.SessionKeys;
        _aesUtility = aesUtility;
        _sessionCrypto = sessionCrypto;
        _loggerFactory = loggerFactory;
        _sessions = new LruCache<SessionCacheKey, SessionMain>(options.CacheSize);
    }
    
    public int TotalSessionCount  => _sessions.Count;

    public SessionMain? CreateSession(SessionType sessionType, byte[] nodeId, IPEndPoint endPoint)
    {
        var key = new SessionCacheKey(nodeId, endPoint);
        var session = _sessions.Get(key);

        if (session == null)
        {
            var newSession = CreateSession(sessionType);
            _sessions.Add(key, newSession);
            session = newSession;
        }
        
        return session;
    }
    
    public SessionMain? GetSession(byte[] nodeId, IPEndPoint endPoint)
    {
        var key = new SessionCacheKey(nodeId, endPoint);

        return _sessions.Get(key);
    }
    
    private SessionMain CreateSession(SessionType sessionType)
    {
        var newSessionKeys = new SessionKeys(_sessionKeys.PrivateKey);
        var cryptoSession = new SessionMain(newSessionKeys, _aesUtility, _sessionCrypto, _loggerFactory, sessionType);
        
        return cryptoSession;
    }
}