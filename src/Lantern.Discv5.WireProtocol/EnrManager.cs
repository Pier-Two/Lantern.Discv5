using System.Net;
using System.Net.Sockets;
using Lantern.Discv5.Enr;
using Lantern.Discv5.Enr.EnrContent;
using Lantern.Discv5.Enr.EnrContent.Entries;
using Lantern.Discv5.Enr.EnrFactory;
using Lantern.Discv5.Enr.IdentityScheme.Interfaces;
using Lantern.Discv5.Enr.IdentityScheme.V4;
using Lantern.Discv5.WireProtocol.Crypto;
using Lantern.Discv5.WireProtocol.Logging.Exceptions;

namespace Lantern.Discv5.WireProtocol;

public class EnrManager
{
    public IIdentitySchemeSigner Signer { get; }

    public EnrRecord Record { get; }

    private EnrManager(IIdentitySchemeSigner signer)
    {
        Signer = signer;
        Record = CreateNewRecord(Signer);
    }
    
    public EnrManager() : this(new IdentitySchemeV4Signer(SessionUtils.GenerateRandomPrivateKey()))
    {
        Record = CreateNewRecord(Signer);
    }

    private static EnrRecord CreateNewRecord(IIdentitySchemeSigner signer)
    {
        var localIpAddress = GetLocalIpAddress();
        
        return new EnrBuilder()
            .WithSigner(signer)
            .WithEntry(EnrContentKey.Id, new EntryId("v4"))
            .WithEntry(EnrContentKey.Ip, new EntryIp(localIpAddress))
            .WithEntry(EnrContentKey.Tcp, new EntryTcp(30303))
            .WithEntry(EnrContentKey.Udp, new EntryUdp(30303))
            .WithEntry(EnrContentKey.Secp256K1, new EntrySecp256K1(signer.PublicKey))
            .Build();
    }
    
    private static IPAddress GetLocalIpAddress()
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                return ip;
            }
        }

        throw new LocalIpAddressNotFoundException("No local IPv4 address found.");
    }
}