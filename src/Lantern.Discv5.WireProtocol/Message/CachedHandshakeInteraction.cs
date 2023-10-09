using System.Diagnostics;

namespace Lantern.Discv5.WireProtocol.Message;

public class CachedHandshakeInteraction
{
    public byte[] NodeId { get; }
    
    public Stopwatch ElapsedTime { get; } 
    
    public CachedHandshakeInteraction(byte[] nodeId)
    {
        NodeId = nodeId;
        ElapsedTime = Stopwatch.StartNew();
    }
}