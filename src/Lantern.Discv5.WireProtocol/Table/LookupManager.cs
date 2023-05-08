using System.Collections.Concurrent;
using System.Net;
using Lantern.Discv5.Enr;
using Lantern.Discv5.Enr.EnrContent;
using Lantern.Discv5.Enr.EnrContent.Entries;
using Lantern.Discv5.Rlp;
using Lantern.Discv5.WireProtocol.Connection;
using Lantern.Discv5.WireProtocol.Identity;
using Lantern.Discv5.WireProtocol.Message;
using Lantern.Discv5.WireProtocol.Packet;
using Lantern.Discv5.WireProtocol.Session;

namespace Lantern.Discv5.WireProtocol.Table;

public class LookupManager : ILookupManager
{
    private readonly IIdentityManager _identityManager;
    private readonly ISessionManager _sessionManager;
    private readonly ITableManager _tableManager;
    private readonly IUdpConnection _udpConnection;
    private readonly IMessageRequester _messageRequester;
    private readonly ConcurrentBag<NodeTableEntry> _receivedNodes = new();
    private readonly int _concurrency;
    private readonly object _nodeLock = new();

    public LookupManager(IIdentityManager identityManager, ISessionManager sessionManager,
        IMessageRequester messageRequester, IUdpConnection udpConnection, ITableManager tableManager,
        int concurrency = 3)
    {
        _identityManager = identityManager;
        _sessionManager = sessionManager;
        _messageRequester = messageRequester;
        _udpConnection = udpConnection;
        _tableManager = tableManager;
        _concurrency = concurrency;
    }

    public async Task<List<NodeTableEntry>> PerformLookup(byte[] targetNodeId, int numberOfPaths = 3)
    {
        var pathQueries = new ConcurrentBag<Task<List<NodeTableEntry>>>();
        var initialNodes = _tableManager.GetInitialNodesForLookup(targetNodeId).ToList();
        var pathBuckets = PartitionInitialNodes(initialNodes, numberOfPaths);

        foreach (var pathBucket in pathBuckets)
        {
            pathQueries.Add(PerformLookupForPath(targetNodeId, pathBucket));
        }
        
        await Task.WhenAll(pathQueries);
        
        var resultSet = new HashSet<NodeTableEntry>(new NodeTableEntryComparer());
        foreach (var query in pathQueries)
        {
            resultSet.UnionWith(await query);
        }
        
        return resultSet
            .OrderBy(node => TableUtility.Log2Distance(node.Id, targetNodeId))
            .Take(_concurrency)
            .ToList();
    }

    private static List<List<NodeTableEntry>> PartitionInitialNodes(IReadOnlyList<NodeTableEntry> initialNodes, int numberOfPaths)
    {
        var pathBuckets = new List<List<NodeTableEntry>>(numberOfPaths);
        for (var i = 0; i < numberOfPaths; i++)
        {
            pathBuckets.Add(new List<NodeTableEntry>());
        }

        for (var i = 0; i < initialNodes.Count; i++)
        {
            pathBuckets[i % numberOfPaths].Add(initialNodes[i]);
        }

        return pathBuckets;
    }

    private async Task<List<NodeTableEntry>> PerformLookupForPath(byte[] targetNodeId, List<NodeTableEntry> pathBucket)
    {
        var closestNodes = new ConcurrentBag<NodeTableEntry>();
        var queriedNodes = new ConcurrentBag<NodeTableEntry>();
        var pendingQueries = new ConcurrentQueue<Task>();
        var pathQueriedNodes = new HashSet<NodeTableEntry>(new NodeTableEntryComparer());

        foreach (var node in pathBucket)
        {
            pendingQueries.Enqueue(SendPacket(MessageType.FindNode, node.Record));
            queriedNodes.Add(node);
            pathQueriedNodes.Add(node);
        }

        while (pendingQueries.Count > 0)
        {
            var query = await Task.WhenAny(pendingQueries);
            pendingQueries.TryDequeue(out _);

            await query;
            var response = new List<NodeTableEntry>();
            while (_receivedNodes.TryTake(out var node))
            {
                response.Add(node);
            }

            foreach (var node in response)
            {
                lock (_nodeLock)
                {
                    if (closestNodes.Count < _concurrency)
                    {
                        closestNodes.Add(node);
                    }
                    else
                    {
                        var farthestClosestNode = closestNodes
                            .OrderByDescending(n => TableUtility.Log2Distance(n.Id, targetNodeId)).First();
                        var newNodeDistance = TableUtility.Log2Distance(node.Id, targetNodeId);

                        if (newNodeDistance < TableUtility.Log2Distance(farthestClosestNode.Id, targetNodeId))
                        {
                            closestNodes.TryTake(out farthestClosestNode);
                            closestNodes.Add(node);
                        }
                    }

                    if (!queriedNodes.Contains(node) && !pathQueriedNodes.Contains(node) &&
                        pendingQueries.Count < _concurrency)
                    {
                        pendingQueries.Enqueue(SendPacket(MessageType.FindNode, node.Record));
                        queriedNodes.Add(node);
                        pathQueriedNodes.Add(node);
                    }
                }
            }
        }

        return closestNodes.OrderByDescending(n => TableUtility.Log2Distance(n.Id, targetNodeId)).Take(_concurrency)
            .ToList();
    }


    public void ReceiveNodesResponse(List<NodeTableEntry> nodes)
    {
        foreach (var node in nodes)
        {
            _receivedNodes.Add(node);
        }
    }

    private async Task SendPacket(MessageType messageType, EnrRecord record)
    {
        var sourceNodeId = _identityManager.Verifier.GetNodeIdFromRecord(_identityManager.Record);
        var destNodeId = _identityManager.Verifier.GetNodeIdFromRecord(record);
        var destEndPoint = new IPEndPoint(record.GetEntry<EntryIp>(EnrContentKey.Ip).Value,
            record.GetEntry<EntryUdp>(EnrContentKey.Udp).Value);
        var maskingIv = RandomUtility.GenerateMaskingIv(PacketConstants.MaskingIvSize);
        var packetNonce = RandomUtility.GenerateNonce(PacketConstants.NonceSize);
        var cryptoSession = _sessionManager.GetSession(destNodeId, destEndPoint);

        if (cryptoSession is { IsEstablished: true })
        {
            var sessionKeys = cryptoSession.CurrentSessionKeys;
            var encryptionKey = cryptoSession.SessionType switch
            {
                SessionType.Initiator => sessionKeys.InitiatorKey,
                SessionType.Recipient => sessionKeys.RecipientKey,
                _ => throw new InvalidOperationException("Invalid session type")
            };
            
            var ordinaryPacket = PacketConstructor.ConstructOrdinaryPacket(sourceNodeId, destNodeId, maskingIv);
            var message = _messageRequester.ConstructMessage(messageType, destNodeId);
            var messageAd = ByteArrayUtils.JoinByteArrays(maskingIv, ordinaryPacket.Result.Item2.GetHeader());
            var encryptedMessage =
                AESUtility.AesGcmEncrypt(encryptionKey, ordinaryPacket.Result.Item2.Nonce, message, messageAd);
            var finalPacket = ByteArrayUtils.JoinByteArrays(ordinaryPacket.Result.Item1, encryptedMessage);
            await _udpConnection.SendAsync(finalPacket, destEndPoint);
            Console.WriteLine("Sent FINDNODES request to " + destEndPoint);
        }
        else
        {
            _sessionManager.SaveHandshakeInteraction(packetNonce, destNodeId);
            var constructedOrdinaryPacket =
                PacketConstructor.ConstructRandomOrdinaryPacket(sourceNodeId, destNodeId, packetNonce, maskingIv);
            await _udpConnection.SendAsync(constructedOrdinaryPacket.Result.Item1, destEndPoint);
            Console.WriteLine("Sent RANDOM packet to initiate handshake with " + destEndPoint);
        }
    }
}