namespace Lantern.Discv5.WireProtocol.Message;

public interface IMessageRequester
{
    byte[]? ConstructPingMessage(byte[] destNodeId);
    
    byte[]? ConstructCachedPingMessage(byte[] destNodeId);
    
    byte[]? ConstructFindNodeMessage(byte[] destNodeId, byte[] targetNodeId);

    byte[]? ConstructCachedFindNodeMessage(byte[] destNodeId, byte[] targetNodeId);

    byte[]? ConstructTalkReqMessage(byte[] destNodeId, byte[] protocol, byte[] request);

    byte[]? ConstructCachedTalkReqMessage(byte[] destNode, byte[] protocol, byte[] request);
}