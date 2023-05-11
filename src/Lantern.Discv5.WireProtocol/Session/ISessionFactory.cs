namespace Lantern.Discv5.WireProtocol.Session;

public interface ISessionFactory
{
    SessionMain Create(SessionType sessionType);
}