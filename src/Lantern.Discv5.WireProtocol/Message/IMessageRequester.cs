namespace Lantern.Discv5.WireProtocol.Message;

public interface IMessageRequester
{
    byte[]? ConstructPingMessage(byte[] destNodeId);
    
    byte[]? ConstructCachedPingMessage(byte[] destNodeId);
    
    byte[]? ConstructFindNodeMessage(byte[] destNodeId, byte[] targetNodeId, bool varyDistance);

    byte[]? ConstructCachedFindNodeMessage(byte[] destNodeId, byte[] targetNodeId, bool varyDistance);

    byte[]? ConstructTalkReqMessage(byte[] destNodeId, bool isRequest = true);

    byte[]? ConstructTalkRespMessage(byte[] data, bool isRequest = true);
}