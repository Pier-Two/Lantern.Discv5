namespace Lantern.Discv5.WireProtocol.Messages;

public interface IMessageDecoder<out T> where T : Message
{
    T DecodeMessage(byte[] message);
}