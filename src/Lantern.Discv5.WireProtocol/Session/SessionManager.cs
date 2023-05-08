using System.Net;

namespace Lantern.Discv5.WireProtocol.Session;

public class SessionManager : ISessionManager
{
    private readonly SessionCache _sessionCache;
    private readonly ISessionKeys _sessionKeys;

    public SessionManager(SessionOptions options)
    {
        _sessionCache = new SessionCache(options.CacheSize, this);
        _sessionKeys = options.SessionKeys;
    }

    public CryptoSession CreateSession(SessionType sessionType, byte[] challengeData)
    {
        Span<byte> privateKey = stackalloc byte[32];
        _sessionKeys.PrivateKey.WriteToSpan(privateKey);
        var sessionKeys = new SessionKeys(privateKey.ToArray());
        var cryptoSession = new CryptoSession(sessionKeys, sessionType)
        {
            ChallengeData = challengeData
        };
        return cryptoSession;
    }
    
    public CryptoSession CreateSession(SessionType sessionType, byte[] nodeId, IPEndPoint endPoint, byte[] challengeData)
    {
        return _sessionCache.CreateSession(sessionType, nodeId, endPoint, challengeData);
    }
    
    public CryptoSession? GetSession(byte[] nodeId, IPEndPoint endPoint)
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