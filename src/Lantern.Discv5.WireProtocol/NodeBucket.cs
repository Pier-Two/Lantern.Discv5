using Makaretu.Collections;

namespace Lantern.Discv5.WireProtocol;

public class NodeBucket : IContact
{
    public byte[] Id { get; }

    public NodeBucket(byte[] id)
    {
        Id = id;
    }
    
}