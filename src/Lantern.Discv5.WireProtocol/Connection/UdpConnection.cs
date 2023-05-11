using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Lantern.Discv5.WireProtocol.Logging.Exceptions;
using IPEndPoint = System.Net.IPEndPoint;
using OperationCanceledException = System.OperationCanceledException;

namespace Lantern.Discv5.WireProtocol.Connection;

public class UdpConnection : IUdpConnection
{
    private const int MaxPacketSize = 1280;
    private const int MinPacketSize = 63;
    private readonly UdpClient _udpClient;
    private readonly ConnectionOptions _connectionOptions;
    private readonly Channel<UdpReceiveResult> _messageChannel = Channel.CreateUnbounded<UdpReceiveResult>();

    public UdpConnection(ConnectionOptions options)
    {
        _connectionOptions = options;
        _udpClient = new UdpClient(new IPEndPoint(IPAddress.Any, options.Port));
        LogConnectionDetails(options);
    }
    
    private void LogConnectionDetails(ConnectionOptions options)
    {
        Console.WriteLine("\nConnection DETAILS");
        Console.WriteLine("==================");
        Console.WriteLine("Listening at endpoint: " + _udpClient.Client.LocalEndPoint);
        Console.WriteLine("External ip address: " + options.ExternalIpAddress);
    }

    public async Task SendAsync(byte[] data, IPEndPoint destination, CancellationToken cancellationToken = default)
    {
        ValidatePacketSize(data);
        cancellationToken.ThrowIfCancellationRequested();

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(_connectionOptions.TimeoutMilliseconds);

        await _udpClient.SendAsync(data, data.Length, destination).ConfigureAwait(false);
    }
    

    public async Task ListenAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var returnedResult = await ReceiveAsync(cancellationToken).ConfigureAwait(false);
                    _messageChannel.Writer.TryWrite(returnedResult);
                }
                catch (UdpTimeoutException)
                {
                    // Do nothing, continue listening
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error receiving packet: {ex}");
                }
            }
        }
        finally
        {
            CompleteMessageChannel();
        }
    }

    public async Task<UdpReceiveResult> ReceiveAsync(CancellationToken cancellationToken = default)
    {
        var receiveResult = await ReceiveAsyncWithTimeout(cancellationToken).ConfigureAwait(false);
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
    }

    public void Dispose()
    {
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
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(_connectionOptions.TimeoutMilliseconds);

        try
        {
            return await _udpClient.ReceiveAsync(cts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException ex)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                /*throw new OperationCanceledException("Receive operation was canceled", cancellationToken);*/
            }

            throw new UdpTimeoutException("Receive timed out");
        }
    }
}