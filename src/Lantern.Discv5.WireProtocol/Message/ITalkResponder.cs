namespace Lantern.Discv5.WireProtocol.Message;

public interface ITalkResponder
{
    bool RespondToRequest(byte[] protocol, byte[] request);

    bool HandleResponse(byte[] response);
}