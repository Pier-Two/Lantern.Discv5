using Lantern.Discv5.Enr;
using Lantern.Discv5.Enr.IdentityScheme.Interfaces;

namespace Lantern.Discv5.WireProtocol.Table;

public class NodeTableEntry
{
    public NodeTableEntry(EnrRecord record, IIdentitySchemeVerifier verifier)
    {
        Record = record;
        Id = verifier.GetNodeIdFromRecord(record);
        IsQueried = false;
        IsLive = false;
    }

    public byte[] Id { get; }
    
    public EnrRecord Record { get; }
    
    public bool IsQueried { get; set; }
    
    public bool IsLive { get; set; }

    public int FailureCounter { get; set; }
    
    public DateTime LastSeen { get; set; }
}