using System.Net;

namespace Lantern.Discv5.WireProtocol.Session;

public interface ISessionFactory
{
    CryptoSession CreateSession(SessionType sessionType, byte[] challengeData);
}