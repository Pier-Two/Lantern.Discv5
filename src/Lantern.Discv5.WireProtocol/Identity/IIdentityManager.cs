using Lantern.Discv5.Enr;
using Lantern.Discv5.Enr.IdentityScheme.Interfaces;

namespace Lantern.Discv5.WireProtocol.Identity;

public interface IIdentityManager
{
    IIdentitySchemeSigner Signer { get; }
    
    IIdentitySchemeVerifier Verifier { get; }
    
    EnrRecord Record { get; }
}