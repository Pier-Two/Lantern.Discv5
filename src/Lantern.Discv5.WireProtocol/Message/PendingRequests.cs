using System.Collections;

namespace Lantern.Discv5.WireProtocol.Message;

public class PendingRequests : IPendingRequests
{
    private readonly Dictionary<byte[], PendingRequest> _pendingRequests;
    
    public PendingRequests()
    {
        _pendingRequests = new Dictionary<byte[], PendingRequest>(ByteArrayEqualityComparer.Instance);
    }
    
    public bool AddPendingRequest(byte[] requestId, PendingRequest? request = default)
    {
        if (ContainsPendingRequest(requestId) == false)
        {
            _pendingRequests.Add(requestId, request);
            return true;
        }
        
        return false;
    }
    
    public bool ContainsPendingRequest(byte[] requestId)
    {
        return _pendingRequests.ContainsKey(requestId);
    }
    
    public PendingRequest? GetPendingRequest(byte[] requestId)
    {
        _pendingRequests.TryGetValue(requestId, out var request);
        return request;
    }
    
    public List<PendingRequest> GetPendingRequests()
    {
        return _pendingRequests.Values.ToList();
    }

    public bool RemovePendingRequest(byte[] requestId)
    {
        if (ContainsPendingRequest(requestId))
        {
            _pendingRequests.Remove(requestId);
            return true;
        }
        
        return false;
    }
    
    private static class ByteArrayEqualityComparer
    {
        public static readonly IEqualityComparer<byte[]> Instance = new ByteArrayEqualityComparerImplementation();

        private class ByteArrayEqualityComparerImplementation : IEqualityComparer<byte[]>
        {
            public bool Equals(byte[]? x, byte[]? y)
            {
                return StructuralComparisons.StructuralEqualityComparer.Equals(x, y);
            }

            public int GetHashCode(byte[] obj)
            {
                return StructuralComparisons.StructuralEqualityComparer.GetHashCode(obj);
            }
        }
    }
}

