using Lantern.Discv5.Enr;
using Lantern.Discv5.Enr.IdentityScheme.Interfaces;
using Lantern.Discv5.WireProtocol.Table.KademliaTable;

namespace Lantern.Discv5.WireProtocol.Table;

public class NodeBucket : IContact
{
    public NodeBucket(EnrRecord record, IIdentitySchemeVerifier verifier)
    {
        Record = record;
        Id = verifier.GetNodeIdFromRecord(record);
    }

    public byte[] Id { get; }
    
    public EnrRecord Record { get; init; }
}