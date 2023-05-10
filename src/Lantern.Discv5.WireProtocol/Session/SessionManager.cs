using System.Net;
using Lantern.Discv5.WireProtocol.Identity;

namespace Lantern.Discv5.WireProtocol.Session;

public class SessionManager : ISessionManager
{
    private readonly SessionCache _sessionCache;
    private readonly ISessionKeys _sessionKeys;
    private readonly IAesUtility _aesUtility;
    private readonly ISessionCrypto _sessionCrypto;

    public SessionManager(SessionOptions options, IAesUtility aesUtility, ISessionCrypto sessionCrypto)
    {
        _sessionCache = new SessionCache(options.CacheSize, this);
        _sessionKeys = options.SessionKeys;
        _aesUtility = aesUtility;
        _sessionCrypto = sessionCrypto;
    }

    public SessionMain CreateSession(SessionType sessionType)
    {
        var newSessionKeys = new SessionKeys(_sessionKeys.PrivateKey);
        var cryptoSession = new SessionMain(newSessionKeys, _aesUtility, _sessionCrypto, sessionType);
        return cryptoSession;
    }
    
    public SessionMain CreateSession(SessionType sessionType, byte[] nodeId, IPEndPoint endPoint)
    {
        return _sessionCache.CreateSession(sessionType, nodeId, endPoint);
    }
    
    public SessionMain? GetSession(byte[] nodeId, IPEndPoint endPoint)
    {
        return _sessionCache.GetSession(nodeId, endPoint);
    }
    
    public bool SaveHandshakeInteraction(byte[] packetNonce, byte[] destNodeId)
    {
        return _sessionCache.AddHandshakeInteraction(packetNonce, destNodeId);
    }
    
    public byte[]? GetHandshakeInteraction(byte[] packetNonce)
    {
        return _sessionCache.GetHandshakeInteraction(packetNonce);
    }
}