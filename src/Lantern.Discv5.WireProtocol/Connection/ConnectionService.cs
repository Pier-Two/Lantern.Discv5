using Lantern.Discv5.WireProtocol.Packet;
using Lantern.Discv5.WireProtocol.Table;

namespace Lantern.Discv5.WireProtocol.Connection;

public sealed class ConnectionService : IConnectionService
{
    private readonly ITableManager _tableManager;
    private readonly IPacketService _packetService;
    private readonly IUdpConnection _udpConnection;
    private readonly CancellationTokenSource _serviceCts;
    private readonly TaskCompletionSource _shutdownTcs;
    private readonly ConnectionOptions _connectionOptions;

    public ConnectionService(ITableManager tableManager, IPacketService packetService, IUdpConnection udpConnection,
        ConnectionOptions connectionOptions)
    {
        _tableManager = tableManager;
        _packetService = packetService;
        _udpConnection = udpConnection;
        _connectionOptions = connectionOptions;
        _serviceCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        _shutdownTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        var listenTask = _udpConnection.ListenAsync(_serviceCts.Token);
        var pingTask = PingNodeAsync(_serviceCts.Token);
        var discoveryTask = StartServiceAsync(_serviceCts.Token);
        var refreshTask = RefreshBucketsAsync(_serviceCts.Token);
        var handleTask = HandleIncomingPacketsAsync(_serviceCts.Token);

        await Task.WhenAny(listenTask, discoveryTask, refreshTask, pingTask, handleTask).ConfigureAwait(false);
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

    private async Task RefreshBucketsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                _tableManager.RefreshTable();
                await Task.Delay(_connectionOptions.RefreshIntervalMilliseconds, cancellationToken)
                    .ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
        finally
        {
            _shutdownTcs.TrySetResult();
        }
    }
    
    private async Task PingNodeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await _packetService.PingNodeAsync().ConfigureAwait(false);
                await Task.Delay(_connectionOptions.PingIntervalMilliseconds, cancellationToken)
                    .ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
        finally
        {
            _shutdownTcs.TrySetResult();
        }
    }
    
    private async Task StartServiceAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await _packetService.RunDiscoveryAsync().ConfigureAwait(false);
                await Task.Delay(_connectionOptions.LookupIntervalMilliseconds, cancellationToken)
                    .ConfigureAwait(false);
            }
        }
        finally
        {
            _shutdownTcs.TrySetResult();
        }
    }

    private async Task HandleIncomingPacketsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await foreach (var packet in _udpConnection.ReadMessagesAsync(cancellationToken).WithCancellation(cancellationToken))
            {
                try
                {
                    await _packetService.HandleReceivedPacket(packet).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    // Do nothing, continue listening for incoming packets
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }
        }
        finally
        {
            _shutdownTcs.SetResult();
        }
    }
}