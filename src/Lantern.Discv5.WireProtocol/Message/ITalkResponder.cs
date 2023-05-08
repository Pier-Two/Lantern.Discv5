namespace Lantern.Discv5.WireProtocol.Message;

public interface ITalkResponder
{
    public bool RespondToRequest(byte[] protocol, byte[] request);

    public bool HandleResponse(byte[] response);
}