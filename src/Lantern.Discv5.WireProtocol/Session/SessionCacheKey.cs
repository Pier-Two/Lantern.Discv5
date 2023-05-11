using System.Net;

namespace Lantern.Discv5.WireProtocol.Session;

public class SessionCacheKey : IEquatable<SessionCacheKey>
{
    public byte[] NodeId { get; }
    public IPEndPoint EndPoint { get; }

    public SessionCacheKey(byte[] nodeId, IPEndPoint endPoint)
    {
        NodeId = new byte[nodeId.Length];
        Array.Copy(nodeId, NodeId, nodeId.Length);
        EndPoint = endPoint;
    }

    public bool Equals(SessionCacheKey? other)
    {
        if (other == null) return false;
        return NodeId.SequenceEqual(other.NodeId) && EndPoint.Equals(other.EndPoint);
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as SessionCacheKey);
    }

    public override int GetHashCode()
    {
        var hash = 17;
        
        unchecked
        {
            hash = NodeId.Aggregate(hash, (current, b) => current * 31 + b.GetHashCode());
            hash = hash * 31 + EndPoint.GetHashCode();
        }

        return hash;
    }
}