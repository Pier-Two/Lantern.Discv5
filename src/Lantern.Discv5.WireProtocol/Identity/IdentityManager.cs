using System.Net;
using System.Net.Sockets;
using Lantern.Discv5.Enr;
using Lantern.Discv5.Enr.EnrContent;
using Lantern.Discv5.Enr.EnrContent.Entries;
using Lantern.Discv5.Enr.IdentityScheme.Interfaces;
using Lantern.Discv5.WireProtocol.Session;
using Microsoft.Extensions.Logging;

namespace Lantern.Discv5.WireProtocol.Identity;

public class IdentityManager: IIdentityManager
{
    private readonly ILogger<IdentityManager> _logger;
    
    public IdentityManager(SessionOptions sessionOptions, IEnrRecord enrRecord, ILoggerFactory loggerFactory)
    {
        Signer = sessionOptions.Signer;
        Verifier = sessionOptions.Verifier;
        Record = enrRecord;
        _logger = loggerFactory.CreateLogger<IdentityManager>();
        _logger.LogInformation("Self ENR record created => {Record}", Record);
    }
    
    public IIdentitySchemeVerifier Verifier { get; }
    
    public IIdentitySchemeSigner Signer { get; }

    public IEnrRecord Record { get; }
    
    public bool IsIpAddressAndPortSet()
    {
        return Record.HasKey(EnrContentKey.Ip) && Record.HasKey(EnrContentKey.Udp) || (Record.HasKey(EnrContentKey.Ip6) && Record.HasKey(EnrContentKey.Udp6));
    }

    public void UpdateIpAddressAndPort(IPEndPoint endpoint)
    {
        if (endpoint.AddressFamily == AddressFamily.InterNetwork)
        {
            Record.UpdateEntry(new EntryIp(endpoint.Address));
            Record.UpdateEntry(new EntryUdp(endpoint.Port));
        }
        else if(endpoint.AddressFamily == AddressFamily.InterNetworkV6)
        {
            Record.UpdateEntry(new EntryIp6(endpoint.Address));
            Record.UpdateEntry(new EntryUdp6(endpoint.Port));
        }
        
        Record.UpdateSignature();
        
        _logger.LogInformation("Self ENR record updated => {Record}", Record);
    }
}