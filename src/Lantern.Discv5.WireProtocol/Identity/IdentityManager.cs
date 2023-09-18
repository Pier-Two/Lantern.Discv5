using System.Net;
using System.Net.Sockets;
using Lantern.Discv5.Enr;
using Lantern.Discv5.Enr.Entries;
using Lantern.Discv5.Enr.Identity;
using Lantern.Discv5.WireProtocol.Session;
using Microsoft.Extensions.Logging;

namespace Lantern.Discv5.WireProtocol.Identity;

public class IdentityManager: IIdentityManager
{
    private readonly ILogger<IdentityManager> _logger;
    
    public IdentityManager(SessionOptions sessionOptions, IEnr enr, ILoggerFactory loggerFactory)
    {
        Signer = sessionOptions.Signer;
        Verifier = sessionOptions.Verifier;
        Record = enr;
        _logger = loggerFactory.CreateLogger<IdentityManager>();
        _logger.LogInformation("Self ENR record created => {Record}", Record);
    }
    
    public IIdentityVerifier Verifier { get; }
    
    public IIdentitySigner Signer { get; }

    public IEnr Record { get; }
    
    public bool IsIpAddressAndPortSet()
    {
        return Record.HasKey(EnrEntryKey.Ip) && Record.HasKey(EnrEntryKey.Udp) || (Record.HasKey(EnrEntryKey.Ip6) && Record.HasKey(EnrEntryKey.Udp6));
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