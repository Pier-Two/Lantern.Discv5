using Lantern.Discv5.Enr;
using Lantern.Discv5.Enr.IdentityScheme.Interfaces;

namespace Lantern.Discv5.WireProtocol.Table;

public class NodeTableEntry
{
    public NodeTableEntry(EnrRecord record, IIdentitySchemeVerifier verifier)
    {
        Record = record;
        Id = verifier.GetNodeIdFromRecord(record);
        Status = NodeStatus.None;
    }

    public byte[] Id { get; }
    
    public EnrRecord Record { get; }

    public NodeStatus Status { get; set; }

    public int FailureCounter { get; set; }
    
    public bool HasRespondedEver { get; set; }
    
    public bool RequestSent { get; set; }
    
    public DateTime LastSeen { get; set; }
}