using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Lantern.Discv5.WireProtocol.Logging.Exceptions;

namespace Lantern.Discv5.WireProtocol.Connection;

public class UdpConnection : IUdpConnection
{
    private const int MaxPacketSize = 1280;
    private const int MinPacketSize = 63;
    private readonly int _timeoutMilliseconds;
    private readonly UdpClient _udpClient;
    private readonly Channel<UdpReceiveResult> _messageChannel = Channel.CreateUnbounded<UdpReceiveResult>();

    public UdpConnection(ConnectionOptions options)
    {
        _timeoutMilliseconds = options.TimeoutMilliseconds;
        _udpClient = new UdpClient(new IPEndPoint(options.IpAddress, options.Port));
    }

    public async Task SendAsync(byte[] data, IPEndPoint destination, CancellationToken cancellationToken = default)
    {
        ValidatePacketSize(data);
        var sendTask = _udpClient.SendAsync(data, data.Length, destination);
        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(_timeoutMilliseconds);
        await sendTask.ConfigureAwait(false);
    }

    public async Task ListenAsync(CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            // Ignore the UdpTimeoutException here and let ReceiveAsync handle it
            try
            {
                var returnedResult = await ReceiveAsync(cancellationToken);
                _messageChannel.Writer.TryWrite(returnedResult);
            }
            catch (UdpTimeoutException)
            {
                // Do nothing, continue listening
            }
        }

        // Propagate the cancellation
        cancellationToken.ThrowIfCancellationRequested();
    }

    public async Task<UdpReceiveResult> ReceiveAsync(CancellationToken cancellationToken = default)
    {
        var receiveResult = await ReceiveAsyncWithTimeout(cancellationToken);
        ValidatePacketSize(receiveResult.Buffer);
        return receiveResult;
    }

    public async IAsyncEnumerable<UdpReceiveResult> ReadMessagesAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var message in _messageChannel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
        {
            yield return message;
        }
    }

    public void CompleteMessageChannel()
    {
        _messageChannel.Writer.TryComplete();
    }

    public void Close()
    {
        _udpClient.Close();
        _udpClient.Dispose();
    }

    private static void ValidatePacketSize(IReadOnlyCollection<byte> data)
    {
        if (data.Count < MinPacketSize) 
            throw new InvalidPacketException("Packet is too small");
        
        if (data.Count > MaxPacketSize) 
            throw new InvalidPacketException("Packet is too large");
    }

    private async Task<UdpReceiveResult> ReceiveAsyncWithTimeout(CancellationToken cancellationToken = default)
    {
        var receiveTask = _udpClient.ReceiveAsync(cancellationToken).AsTask();
        var timeoutTask = Task.Delay(_timeoutMilliseconds, cancellationToken);
        var completedTask = await Task.WhenAny(receiveTask, timeoutTask);

        if (completedTask != timeoutTask) 
            return await receiveTask;
        
        if (timeoutTask.IsCanceled)
        {
            throw new OperationCanceledException("Receive operation was canceled", cancellationToken);
        } 
        
        throw new UdpTimeoutException("Receive timed out");
    }
}