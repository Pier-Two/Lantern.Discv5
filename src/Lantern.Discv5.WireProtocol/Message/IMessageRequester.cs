using Lantern.Discv5.WireProtocol.Packet.Headers;

namespace Lantern.Discv5.WireProtocol.Message;

public interface IMessageRequester
{ 
    byte[]? ConstructMessage(MessageType messageType, byte[] destNodeId);
}