namespace Lantern.Discv5.WireProtocol.Message;

public interface IMessageDecoder
{
    Message DecodeMessage(byte[] message);
}