using System.Diagnostics;

namespace Lantern.Discv5.WireProtocol.Message;

public class CachedHandshakeInteraction(byte[] nodeId)
{
    public byte[] NodeId { get; } = nodeId;

    public Stopwatch ElapsedTime { get; } = Stopwatch.StartNew();
}