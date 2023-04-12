namespace Lantern.Discv5.Enr.IdentityScheme.Interfaces;

public interface IIdentitySchemeVerifier
{
    bool VerifyRecord(EnrRecord record);
    
    byte[] GetNodeIdFromRecord(EnrRecord record);
}