using System.Net;
using System.Net.Sockets;
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
    private IIdentitySchemeSigner Signer { get; }
    
    public IIdentitySchemeVerifier Verifier { get; }

    public EnrRecord Record { get; }
    
    public byte[] NodeId => Verifier.GetNodeIdFromRecord(Record);
    
    public IdentityManager(ConnectionOptions connectionOptions, SessionOptions sessionOptions)
    {
        Signer = sessionOptions.Signer;
        Verifier = sessionOptions.Verifier;
        Record = CreateNewRecord(connectionOptions, Signer);
    }

    public bool IsIpAddressAndPortSet()
    {
        return Record.HasKey(EnrContentKey.Ip) && Record.HasKey(EnrContentKey.Udp) || (Record.HasKey(EnrContentKey.Ip6) && Record.HasKey(EnrContentKey.Udp6));
    }

    public void UpdateIpAddressAndPort(IPEndPoint endpoint)
    {
        if (endpoint.AddressFamily == AddressFamily.InterNetwork)
        {
            Record.UpdateEntry(EnrContentKey.Ip, new EntryIp(endpoint.Address));
            Record.UpdateEntry(EnrContentKey.Udp, new EntryUdp(endpoint.Port));
        }
        else if(endpoint.AddressFamily == AddressFamily.InterNetworkV6)
        {
            Record.UpdateEntry(EnrContentKey.Ip6, new EntryIp6(endpoint.Address));
            Record.UpdateEntry(EnrContentKey.Udp6, new EntryUdp6(endpoint.Port));
        }
        
        Record.UpdateSignature(Signer);
        Console.Write("\nSelf ENR record updated => " + Record);
    }

    private static EnrRecord CreateNewRecord(ConnectionOptions options, IIdentitySchemeSigner signer)
    {
        if (options.ExternalIpAddress != null)
        {
            return new EnrBuilder()
                .WithSigner(signer)
                .WithEntry(EnrContentKey.Id, new EntryId("v4")) // Replace with a constant
                .WithEntry(EnrContentKey.Ip, new EntryIp(options.ExternalIpAddress)) // Should use external IP address
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