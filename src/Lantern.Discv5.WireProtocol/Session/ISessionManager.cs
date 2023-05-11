using System.Net;

namespace Lantern.Discv5.WireProtocol.Session;

public interface ISessionManager
{
    public SessionMain? GetSession(byte[] nodeId, IPEndPoint endPoint);

    public SessionMain? CreateSession(SessionType sessionType, byte[] nodeId, IPEndPoint endPoint);

    public bool SaveHandshakeInteraction(byte[] packetNonce, byte[] destNodeId);
    
    public byte[]? GetHandshakeInteraction(byte[] packetNonce);
}