namespace Lantern.Discv5.Enr.IdentityScheme.Interfaces;

public interface IIdentitySchemeSigner
{
    byte[] PublicKey { get; }
    
    byte[] SignRecord(IEnrRecord record);
}