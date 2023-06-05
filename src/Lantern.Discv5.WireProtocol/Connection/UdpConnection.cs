using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Lantern.Discv5.WireProtocol.Logging.Exceptions;
using Microsoft.Extensions.Logging;
using IPEndPoint = System.Net.IPEndPoint;
using OperationCanceledException = System.OperationCanceledException;

namespace Lantern.Discv5.WireProtocol.Connection;

public class UdpConnection : IUdpConnection, IDisposable
{
    private const int MaxPacketSize = 1280;
    private const int MinPacketSize = 63;
    private const int UdpTimeoutMilliseconds = 2000;
    private readonly UdpClient _udpClient;
    private readonly ILogger<UdpConnection> _logger;
    private readonly Channel<UdpReceiveResult> _messageChannel = Channel.CreateUnbounded<UdpReceiveResult>();

    public UdpConnection(ConnectionOptions options, ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<UdpConnection>(); 
        _udpClient = new UdpClient(new IPEndPoint(IPAddress.Any, options.Port));
        _logger.LogInformation("UdpConnection initialized on port {Port}", options.Port);
    }

    public async Task SendAsync(byte[] data, IPEndPoint destination, CancellationToken token = default)
    {
        ValidatePacketSize(data);
        token.ThrowIfCancellationRequested();

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
        cts.CancelAfter(UdpTimeoutMilliseconds);

        _logger.LogDebug("Sending packet to {Destination}", destination);
        await _udpClient.SendAsync(data, data.Length, destination).ConfigureAwait(false);
    }
    
    public async Task ListenAsync(CancellationToken token = default)
    {
        _logger.LogInformation("Starting ListenAsync");
        try
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var returnedResult = await ReceiveAsync(token).ConfigureAwait(false);
                    _messageChannel.Writer.TryWrite(returnedResult);
                }
                catch (UdpTimeoutException)
                {
                    // Do nothing, continue listening
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error receiving packet");
                }
            }
        }
        finally
        {
            _logger.LogInformation("Stopped ListenAsync");
            CompleteMessageChannel();
        }
    }

    public async Task<UdpReceiveResult> ReceiveAsync(CancellationToken token = default)
    {
        var receiveResult = await ReceiveAsyncWithTimeout(token).ConfigureAwait(false);
        ValidatePacketSize(receiveResult.Buffer);
        
        _logger.LogDebug("Received packet from {Source}", receiveResult.RemoteEndPoint);
        return receiveResult;
    }

    public async IAsyncEnumerable<UdpReceiveResult> ReadMessagesAsync([EnumeratorCancellation] CancellationToken token = default)
    {
        await foreach (var message in _messageChannel.Reader.ReadAllAsync(token).ConfigureAwait(false))
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
        _logger.LogInformation("Closing UdpConnection");
        _udpClient.Close();
    }

    public void Dispose()
    {
        _logger.LogInformation("Disposing UdpConnection");
        _udpClient.Dispose();
    }

    private static void ValidatePacketSize(IReadOnlyCollection<byte> data)
    {
        if (data.Count < MinPacketSize) 
            throw new InvalidPacketException("Packet is too small");

        if (data.Count > MaxPacketSize)
            throw new InvalidPacketException("Packet is too large");
    }

    private async Task<UdpReceiveResult> ReceiveAsyncWithTimeout(CancellationToken token = default)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
        cts.CancelAfter(UdpTimeoutMilliseconds);

        try
        {
            return await _udpClient.ReceiveAsync(cts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            if (token.IsCancellationRequested)
            {
                _logger.LogInformation("ReceiveAsync was cancelled gracefully");
            }

            throw new UdpTimeoutException("Receive timed out");
        }
    }
}