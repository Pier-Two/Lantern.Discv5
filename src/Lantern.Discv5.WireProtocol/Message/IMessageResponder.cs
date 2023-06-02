using System.Net;

namespace Lantern.Discv5.WireProtocol.Message;

public interface IMessageResponder
{ 
    Task<byte[]?> HandleMessage(byte[] message, IPEndPoint endPoint);
}