using Makaretu.Collections;

namespace Lantern.Discv5.WireProtocol;

public class NodeBucket : IContact
{
    public NodeBucket(byte[] id)
    {
        Id = id;
    }

    public byte[] Id { get; }
}