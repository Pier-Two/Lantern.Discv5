using System.Net;

namespace Lantern.Discv5.WireProtocol.Message;

public interface IMessageResponder
{ 
    Task<byte[]?> HandleMessageAsync(byte[] message, IPEndPoint endPoint);
}