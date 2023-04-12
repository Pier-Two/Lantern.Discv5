using System.Net;
using System.Net.Sockets;
using Lantern.Discv5.WireProtocol.Logging.Exceptions;

namespace Lantern.Discv5.WireProtocol.Connection;

public class UdpConnection : IDisposable
{
    private const int MaxPacketSize = 1280;
    private const int MinPacketSize = 63;
    private readonly int _timeoutMilliseconds;
    private readonly UdpClient _udpClient;

    public UdpConnection(int port, int timeoutMilliseconds = 500)
    {
        _timeoutMilliseconds = timeoutMilliseconds;
        _udpClient = new UdpClient(port);
    }

    public async Task SendAsync(byte[] data, IPEndPoint destination, CancellationToken cancellationToken = default)
    {
        ValidatePacketSize(data);
        var sendTask = _udpClient.SendAsync(data, data.Length, destination);
        var timeoutTask = Task.Delay(_timeoutMilliseconds, cancellationToken);
        var completedTask = await Task.WhenAny(sendTask, timeoutTask);

        if (completedTask == timeoutTask) 
            throw new UdpTimeoutException("Send timed out");
    }

    public async Task<byte[]> ReceiveAsync(CancellationToken cancellationToken = default)
    {
        var receiveResult = await ReceiveAsyncWithTimeout(cancellationToken);
        ValidatePacketSize(receiveResult.Buffer);
        return receiveResult.Buffer;
    }
    
    public void Close()
    {
        _udpClient.Close();
        Dispose();
    }
    
    public void Dispose()
    {
        _udpClient.Dispose();
    }

    private static void ValidatePacketSize(byte[] data)
    {
        if (data.Length < MinPacketSize) throw new InvalidPacketException("Packet is too small");
        if (data.Length > MaxPacketSize) throw new InvalidPacketException("Packet is too large");
    }

    private async Task<UdpReceiveResult> ReceiveAsyncWithTimeout(CancellationToken cancellationToken = default)
    {
        var receiveTask = _udpClient.ReceiveAsync(cancellationToken).AsTask();
        var timeoutTask = Task.Delay(_timeoutMilliseconds, cancellationToken);
        var completedTask = await Task.WhenAny(receiveTask, timeoutTask);

        if (completedTask == timeoutTask)
        {
            if (timeoutTask.IsCanceled)
            {
                throw new OperationCanceledException("Receive operation was canceled", cancellationToken);
            } 
            throw new UdpTimeoutException("Receive timed out");
        }

        return await receiveTask;
    }
}