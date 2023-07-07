namespace Lantern.Discv5.WireProtocol.Message;

public interface IMessageRequester
{
    byte[]? ConstructPingMessage(byte[] destNodeId);

    byte[]? ConstructFindNodeMessage(byte[] destNodeId, byte[] targetNodeId);

    byte[]? ConstructTalkReqMessage(byte[] destNodeId, byte[] protocol, byte[] request);

    byte[]? ConstructTalkRespMessage(byte[] destNodeId, byte[] response);
}