using Lantern.Discv5.Enr;
using Lantern.Discv5.WireProtocol.Connection;
using Lantern.Discv5.WireProtocol.Messages;
using Lantern.Discv5.WireProtocol.Messages.Responses;
using Lantern.Discv5.WireProtocol.Table;
using Microsoft.Extensions.Logging;

namespace Lantern.Discv5.WireProtocol.Packet;

public class PacketReceiver(IPacketManager packetManager,
    ConnectionOptions connectionOptions,
    IMessageDecoder messageDecoder,
    ILoggerFactory loggerFactory) : IPacketReceiver
{
    private readonly ILogger<PacketReceiver> _logger = loggerFactory.CreateLogger<PacketReceiver>();
    
    public event EventHandler<PongResponseEventArgs>? PongResponseReceived; 
    
    public event EventHandler<NodesResponseEventArgs>? NodesResponseReceived;
    
    public async Task<PongMessage?> SendPingAsync(IEnr dest)
    {
        var payload = await packetManager.SendPacket(dest, MessageType.Ping);
        
        if (payload is null)
        {
            return null;
        }
        
        var message = messageDecoder.DecodeMessage(payload);
        var tcs = new TaskCompletionSource<PongMessage>();
        
        PongResponseReceived += HandlePongResponse;
        
        var delayTask = Task.Delay(connectionOptions.ReceiveTimeoutMs);
        var completedTask = await Task.WhenAny(tcs.Task, delayTask);
        
        if (completedTask != delayTask) 
            return await tcs.Task;
        
        _logger.LogWarning("PING request to {NodeId} timed out", dest.NodeId);
        PongResponseReceived -= HandlePongResponse;
        return null;
        
        void HandlePongResponse(object? sender, PongResponseEventArgs e)
        {
            if (!e.RequestId.SequenceEqual(message.RequestId)) 
                return;
            
            tcs.SetResult(e.PongMessage);
            PongResponseReceived -= HandlePongResponse;
        }
    }

    public async Task<IEnr[]?> SendFindNodeAsync(IEnr dest, byte[] targetNodeId)
    {
        var payload = await packetManager.SendPacket(dest, MessageType.FindNode, targetNodeId);

        if (payload is null)
        {
            return null;
        }

        var message = messageDecoder.DecodeMessage(payload);
        var tcs = new TaskCompletionSource<IEnr[]>();

        NodesResponseReceived += HandleNodesResponse;

        var delayTask = Task.Delay(connectionOptions.ReceiveTimeoutMs);
        var completedTask = await Task.WhenAny(tcs.Task, delayTask);

        if (completedTask != delayTask) 
            return await tcs.Task;
        
        _logger.LogWarning("FINDNODE request to {NodeId} timed out", Convert.ToHexString(targetNodeId));
        NodesResponseReceived -= HandleNodesResponse;
        return null;

        void HandleNodesResponse(object? sender, NodesResponseEventArgs e)
        {
            if (!e.RequestId.SequenceEqual(message.RequestId)) 
                return;
            
            tcs.SetResult(e.Nodes.Select(entry => (IEnr)entry.Record).ToArray());
            NodesResponseReceived -= HandleNodesResponse;
        }
    }

    public async Task SendTalkReqAsync(IEnr dest, byte[] topic, byte[] message)
    {
        throw new NotImplementedException();
    }
    
    public async Task SendTalkRespAsync(IEnr dest, byte[] requestId, byte[] message)
    {
        throw new NotImplementedException();
    }
    
    public void RaisePongResponseReceived(PongResponseEventArgs e)
    {
        PongResponseReceived?.Invoke(this, e);
    }
    
    public void RaiseNodesResponseReceived(NodesResponseEventArgs e)
    {
        NodesResponseReceived?.Invoke(this, e);
    }
}