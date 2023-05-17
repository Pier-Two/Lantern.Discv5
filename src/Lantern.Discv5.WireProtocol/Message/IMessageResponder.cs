using System.Net;

namespace Lantern.Discv5.WireProtocol.Message;

public interface IMessageResponder
{
    byte[]? HandleMessage(byte[] message, IPEndPoint endPoint);
}