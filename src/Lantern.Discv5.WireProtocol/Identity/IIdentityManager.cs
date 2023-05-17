using System.Net;
using Lantern.Discv5.Enr;
using Lantern.Discv5.Enr.IdentityScheme.Interfaces;

namespace Lantern.Discv5.WireProtocol.Identity;

public interface IIdentityManager
{
    IIdentitySchemeVerifier Verifier { get; }
    
    EnrRecord Record { get; }
    
    byte[] NodeId { get; }
    
    bool IsIpAddressAndPortSet();

    void UpdateIpAddressAndPort(IPEndPoint endpoint);
}