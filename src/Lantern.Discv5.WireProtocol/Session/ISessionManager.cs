using System.Net;

namespace Lantern.Discv5.WireProtocol.Session;

public interface ISessionManager
{
    public CryptoSession? GetSession(byte[] nodeId, IPEndPoint endPoint);
    
    public CryptoSession CreateSession(SessionType sessionType, byte[] challengeData);
    
    public CryptoSession CreateSession(SessionType sessionType, byte[] nodeId, IPEndPoint endPoint, byte[] challengeData);

    public bool SaveHandshakeInteraction(byte[] packetNonce, byte[] destNodeId);
    
    public byte[]? GetHandshakeInteraction(byte[] packetNonce);
}