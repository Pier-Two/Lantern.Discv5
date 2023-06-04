namespace Lantern.Discv5.WireProtocol.Message;

public interface ITalkReqAndRespHandler
{
    bool HandleRequest(byte[] protocol, byte[] request);

    bool HandleResponse(byte[] response);
}