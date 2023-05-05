namespace Lantern.Discv5.WireProtocol.Message;

public interface IMessageDecoder<out T> where T : Message
{
    T DecodeMessage(byte[] message);
}