using Lantern.Discv5.WireProtocol.Packets;

namespace Lantern.Discv5.WireProtocol.Connection;

public sealed class ConnectionService
{
    private readonly IPacketService _packetService;
    private readonly IUdpConnection _udpConnection;
    private readonly CancellationTokenSource _serviceCts;
    private readonly TaskCompletionSource _shutdownTcs;
    private readonly ConnectionOptions _connectionOptions;

    public ConnectionService(IPacketService packetService, IUdpConnection udpConnection, ConnectionOptions connectionOptions)
    {
        _packetService = packetService;
        _udpConnection = udpConnection;
        _connectionOptions = connectionOptions;
        _serviceCts = new CancellationTokenSource();
        _shutdownTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    public async Task StartAsync()
    {
        var listenTask = _udpConnection.ListenAsync(_serviceCts.Token);
        var lookupTask = StartServiceAsync(_serviceCts.Token);
        var handleTask = HandleIncomingPacketsAsync(_serviceCts.Token);

        await Task.WhenAny(listenTask, handleTask, lookupTask, Task.Delay(_connectionOptions.TimeoutMilliseconds, _serviceCts.Token)).ConfigureAwait(false);
        _udpConnection.CompleteMessageChannel();
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _serviceCts.Token);
        cts.Cancel();

        try
        {
            await _shutdownTcs.Task.ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Handle the scenario when the StopAsync method itself is canceled
        }
        finally
        {
            _serviceCts.Dispose();
        }
    }

    private async Task StartServiceAsync(CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await _packetService.SendOrdinaryPacketForLookup(_udpConnection).ConfigureAwait(false);
            await Task.Delay(_connectionOptions.LookupIntervalMilliseconds, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task HandleIncomingPacketsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await foreach (var packet in _udpConnection.ReadMessagesAsync(cancellationToken).WithCancellation(cancellationToken))
            {
                await _packetService.HandleReceivedPacket(_udpConnection, packet).ConfigureAwait(false);
            }
        }
        finally
        {
            _shutdownTcs.SetResult();
        }
    }
}