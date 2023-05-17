using System.Net;

namespace Lantern.Discv5.WireProtocol.Session;

public interface ISessionManager
{
    int TotalSessionCount { get; }
    
    SessionMain? GetSession(byte[] nodeId, IPEndPoint endPoint);
    
    SessionMain? CreateSession(SessionType sessionType, byte[] nodeId, IPEndPoint endPoint);

    void SaveHandshakeInteraction(byte[] packetNonce, byte[] destNodeId);
    
    byte[]? GetHandshakeInteraction(byte[] packetNonce);
}