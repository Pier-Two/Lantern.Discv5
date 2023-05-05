namespace Lantern.Discv5.WireProtocol.Message;

public interface IMessageHandler
{
    public byte[]? HandleMessage(byte[] message);
}