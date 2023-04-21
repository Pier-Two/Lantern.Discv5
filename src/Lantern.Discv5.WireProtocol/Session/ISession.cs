namespace Lantern.Discv5.WireProtocol.Session;

public interface ISession
{
    public byte[] EncryptMessage(byte[] message);
    
    public byte[] DecryptMessage(byte[] message);
}