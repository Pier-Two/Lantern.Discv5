using Lantern.Discv5.WireProtocol.Packet.Headers;

namespace Lantern.Discv5.WireProtocol.Message;

public interface IMessageRequester
{
    byte[]? ConstructPingMessage(byte[] destNodeId, bool isRequest = true);

    byte[]? ConstructFindNodeMessage(byte[] destNodeId, byte[] targetNodeId, bool isRequest = true);

    byte[]? ConstructTalkReqMessage(byte[] destNodeId, bool isRequest = true);

    byte[]? ConstructTalkRespMessage(byte[] data, bool isRequest = true);
    
    CachedRequest? GetCachedRequest(byte[] nodeId);
    
    void RemoveCachedRequest(byte[] nodeId);

    byte[] CreateFromCachedRequest(CachedRequest request);
}