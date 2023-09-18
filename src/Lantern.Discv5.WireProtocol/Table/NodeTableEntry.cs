using Lantern.Discv5.Enr;
using Lantern.Discv5.Enr.Identity;

namespace Lantern.Discv5.WireProtocol.Table;

public class NodeTableEntry
{
    public NodeTableEntry(IEnr record, IIdentityVerifier verifier)
    {
        Record = record;
        Id = verifier.GetNodeIdFromRecord(record);
        Status = NodeStatus.None;
    }

    public byte[] Id { get; }
    
    public IEnr Record { get; }

    public NodeStatus Status { get; set; }

    public int FailureCounter { get; set; }
    
    public bool HasRespondedEver { get; set; }
    
    public DateTime LastSeen { get; set; }
}