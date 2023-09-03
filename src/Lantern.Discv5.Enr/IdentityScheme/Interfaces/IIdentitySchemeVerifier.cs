namespace Lantern.Discv5.Enr.IdentityScheme.Interfaces;

public interface IIdentitySchemeVerifier
{
    bool VerifyRecord(IEnrRecord record);
    
    byte[] GetNodeIdFromRecord(IEnrRecord record);
}