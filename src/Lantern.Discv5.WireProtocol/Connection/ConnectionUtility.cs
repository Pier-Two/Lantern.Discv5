using System.Net;
using System.Net.Sockets;
using Lantern.Discv5.WireProtocol.Logging.Exceptions;
using Open.Nat;

namespace Lantern.Discv5.WireProtocol.Connection;

public static class ConnectionUtility
{
    public static IPAddress GetLocalIpAddress()
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
    
    public static async Task<IPAddress> DetermineExternalIpAddress()
    {
        
        var discoverer = new NatDiscoverer();
        var cts = new CancellationTokenSource(5000); // Set a timeout for the discovery process
        var device = await discoverer.DiscoverDeviceAsync(PortMapper.Upnp, cts);
        return await device.GetExternalIPAsync();
    }
}