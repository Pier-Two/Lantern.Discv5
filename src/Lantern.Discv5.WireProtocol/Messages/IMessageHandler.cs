namespace Lantern.Discv5.WireProtocol.Messages;

public interface IMessageHandler
{
    public void HandleMessage(byte[] message);
}