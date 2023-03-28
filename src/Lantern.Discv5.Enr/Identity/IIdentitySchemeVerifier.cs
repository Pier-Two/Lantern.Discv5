namespace Lantern.Discv5.Enr.Identity;

public interface IIdentitySchemeVerifier
{
    bool VerifyRecord(EnrRecord record);
    
    byte[] GetNodeIdFromRecord(EnrRecord record);
}