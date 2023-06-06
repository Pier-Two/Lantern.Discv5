namespace Lantern.Discv5.WireProtocol.Message;

public interface ITalkReqAndRespHandler
{
    byte[]? HandleRequest(byte[] protocol, byte[] request);

    byte[]? HandleResponse(byte[] response);
}