using System.Net;
using System.Net.Sockets;

namespace Lantern.Discv5.WireProtocol.Connection;

public interface IUdpConnection
{
    Task SendAsync(byte[] data, IPEndPoint destination, CancellationToken cancellationToken = default);
    
    Task ListenAsync(CancellationToken cancellationToken);
    
    IAsyncEnumerable<UdpReceiveResult> ReadMessagesAsync(CancellationToken cancellationToken = default);

    void CompleteMessageChannel();
}