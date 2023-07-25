using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Lantern.Discv5.WireProtocol.Logging.Exceptions;
using Lantern.Discv5.WireProtocol.Packet;
using Lantern.Discv5.WireProtocol.Utility;
using Microsoft.Extensions.Logging;
using IPEndPoint = System.Net.IPEndPoint;
using OperationCanceledException = System.OperationCanceledException;

namespace Lantern.Discv5.WireProtocol.Connection;

public class UdpConnection : IUdpConnection
{
    private readonly UdpClient _udpClient;
    private readonly ILogger<UdpConnection> _logger;
    private readonly Channel<UdpReceiveResult> _messageChannel; 
    private readonly IGracefulTaskRunner _taskRunner;
  
    public UdpConnection(ConnectionOptions options, ILoggerFactory loggerFactory, IGracefulTaskRunner taskRunner)
    {
        _udpClient = new UdpClient(new IPEndPoint(IPAddress.Any, options.Port));
        _messageChannel = Channel.CreateUnbounded<UdpReceiveResult>();
        _logger = loggerFactory.CreateLogger<UdpConnection>(); 
        _taskRunner = taskRunner;
    }

    public async Task SendAsync(byte[] data, IPEndPoint destination)
    {
        ValidatePacketSize(data);
        _logger.LogDebug("Sending packet to {Destination}", destination);
        
        await _udpClient.SendAsync(data, data.Length, destination).ConfigureAwait(false);
    }
    
    public async Task ListenAsync(CancellationToken token = default)
    {
        _logger.LogInformation("Starting ListenAsync");
        
        await _taskRunner.RunWithGracefulCancellationAsync(async cancellationToken => 
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var returnedResult = await ReceiveAsync(cancellationToken).ConfigureAwait(false);
                    _messageChannel.Writer.TryWrite(returnedResult);
                }
            }, "Listen", token);
    }

    public async IAsyncEnumerable<UdpReceiveResult> ReadMessagesAsync([EnumeratorCancellation] CancellationToken token = default)
    {
        await foreach (var message in _messageChannel.Reader.ReadAllAsync(token).ConfigureAwait(false))
        {
            yield return message;
        }
    }

    public void Close()
    {
        _logger.LogInformation("Closing UdpConnection");
        _udpClient.Close();
        _udpClient.Dispose();
        _messageChannel.Writer.TryComplete();
    }

    public static void ValidatePacketSize(IReadOnlyCollection<byte> data)
    {
        switch (data.Count)
        {
            case < PacketConstants.MinPacketSize:
                throw new InvalidPacketException("Packet is too small");
            case > PacketConstants.MaxPacketSize:
                throw new InvalidPacketException("Packet is too large");
        }
    }
    
    private async Task<UdpReceiveResult> ReceiveAsync(CancellationToken token = default)
    {
        var receiveResult = await _udpClient.ReceiveAsync(token).ConfigureAwait(false);
        ValidatePacketSize(receiveResult.Buffer);
        
        _logger.LogDebug("Received packet from {Source}", receiveResult.RemoteEndPoint);
        return receiveResult;
    }
}