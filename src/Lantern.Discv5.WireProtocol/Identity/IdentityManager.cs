using Lantern.Discv5.Enr;
using Lantern.Discv5.Enr.EnrContent;
using Lantern.Discv5.Enr.EnrContent.Entries;
using Lantern.Discv5.Enr.EnrFactory;
using Lantern.Discv5.Enr.IdentityScheme.Interfaces;
using Lantern.Discv5.Enr.IdentityScheme.V4;
using Lantern.Discv5.WireProtocol.Connection;
using Lantern.Discv5.WireProtocol.Session;

namespace Lantern.Discv5.WireProtocol.Identity;

public class IdentityManager: IIdentityManager
{
    public IIdentitySchemeSigner Signer { get; }
    
    public IIdentitySchemeVerifier Verifier { get; }

    public EnrRecord Record { get; }
    
    public IdentityManager(ConnectionOptions connectionOptions, SessionOptions? sessionOptions = null)
    {
        Signer = sessionOptions?.Signer ?? new IdentitySchemeV4Signer(SessionUtility.GenerateRandomPrivateKey());
        Verifier = sessionOptions?.Verifier ?? new IdentitySchemeV4Verifier();
        Record = CreateNewRecord(connectionOptions, Signer);
    }

    private static EnrRecord CreateNewRecord(ConnectionOptions options, IIdentitySchemeSigner signer)
    {
        if (options.ExternalIpAddress != null)
        {
            return new EnrBuilder()
                .WithSigner(signer)
                .WithEntry(EnrContentKey.Id, new EntryId("v4")) // Replace with a constant
                .WithEntry(EnrContentKey.Ip, new EntryIp(options.ExternalIpAddress)) // Should use external IP address
                .WithEntry(EnrContentKey.Tcp, new EntryTcp(options.Port))
                .WithEntry(EnrContentKey.Udp, new EntryUdp(options.Port))
                .WithEntry(EnrContentKey.Secp256K1, new EntrySecp256K1(signer.PublicKey))
                .Build();
        }
        
        return new EnrBuilder()
            .WithSigner(signer)
            .WithEntry(EnrContentKey.Id, new EntryId("v4")) // Replace with a constant
            .WithEntry(EnrContentKey.Secp256K1, new EntrySecp256K1(signer.PublicKey))
            .Build();
    }
}