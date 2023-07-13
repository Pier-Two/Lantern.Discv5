This document provides guidance on how to use the Lantern.Discv5 library in a C# project.

## General Usage

#### Instantiating Discv5 Protocol

The `Discv5Protocol` class provides an interface for interacting with the Ethereum Discovery Protocol Version 5. Start by creating an instance of `Discv5Protocol`:

```csharp
using Lantern.Discv5;

var bootstrapEnrs = new string[] { 
    "enr:-Ku4QI...", 
    "enr:-KG4QO..." // Include bootstrap Ethereum Node Records (ENR)
};

var discv5 = Discv5Builder.CreateDefault(bootstrapEnrs);
```

The `CreateDefault` method of `Discv5Builder` is used to instantiate the Discv5 protocol with a list of bootstrap ENR nodes. 

#### Implementing Custom Subprotocols

For applications that require custom subprotocols, you can pass an instance of a class that implements `ITalkReqAndRespHandler` to the `CreateDefault` method. This class can handle application-specific subprotocols within the TalkReq and TalkResp messages :

```csharp
public class CustomHandler : ITalkReqAndRespHandler
{
    public byte[]? HandleRequest(byte[] protocol, byte[] request)
    {
        // Handle the incoming TalkReq request here
        // Return response payload or null
    }

    public byte[]? HandleResponse(byte[] response)
    {
        // Handle the incoming TalkResp response here
        // Return processed response or null
    }
}
```

Then the Discv5 protocol can be instantiated by passing the custom handler:

```csharp
ITalkReqAndRespHandler customHandler = new CustomHandler(); 

var discv5 = Discv5Builder.CreateDefault(bootstrapEnrs, customHandler);
```

#### Starting the Protocol

`Discv5Protocol` exposes a `StartProtocolAsync` method to start the discovery protocol.

```csharp
discv5.StartProtocolAsync();
```

#### Stopping the Protocol

The `StopProtocolAsync` method can be used to stop the discovery protocol when it's no longer needed.

```csharp
await discv5.StopProtocolAsync();
```

#### Info about Current Node

The `SelfEnrRecord` property returns the Ethereum Node Record (ENR) for the local node.

```csharp
EnrRecord selfRecord = discv5.SelfEnrRecord;
```

#### Node/Peer Counts

The `NodesCount` method returns the total number of nodes that are stored in the routing table.

```csharp
int totalNodes = discv5.NodesCount();
```

The `PeerCount` method returns the number of active nodes (peers) currently connected to.

```csharp
int activePeers = discv5.PeerCount();
```

#### Sending Ping Request

A PING message can be sent to node by supplying its ENR record using the method:

```csharp
await discv5.SendPingAsync(destinationEnrRecord);
```

#### Sending Find Node Request

A FINDNODE request can be sent to a specific peer to discover new peers:

```csharp
byte[] nodeId = ...; // Node identifier
await discv5.SendFindNodeAsync(destinationEnrRecord, nodeId);
```

#### Performing Node Lookup

To query the network for nodes closest to a given target Node ID, you can use the `PerformLookupAsync` method. This method will return list of `NodeTableEntry` objects, or `null` if no nodes were found or an error occurred.

```csharp
byte[] targetNodeId = ...; // Target node identifier
List<NodeTableEntry>? closestNodes = await discv5.PerformLookupAsync(targetNodeId);
```

In this example, `targetNodeId` is the Node ID of your target and must be a 32-byte long byte array. `PerformLookupAsync` will locate the nodes in the network that are closest to your target Node ID. This function will return a list of nodes or `null` if an error occurred or no nodes were found.

#### Sending Talk Requests (TalkReq and TalkResp)

TalkReq/TalkResp messages can be sent for general-purpose communication.

```csharp
byte[] protocol = ...; // Identify Protocol
byte[] request = ...; // message payload
await discv5.SendTalkReqAsync(destinationEnrRecord, protocol, request);
```

## Advanced Usage
For a more detailed description for each of these options, please visit the [Options](OPTIONS.md) documentation.
#### ConnectionOptions Configuration

`ConnectionOptions` allows you to adjust certain settings related with the connection manager. Example:

```csharp
var connectionOptions = new ConnectionOptions.Builder()
  .WithPort(9001)
  .WithReqRespTimeoutMs(2000)
  .Build();
```

#### SessionOptions Configuration

`SessionOptions` allows you to adjust certain settings related with the session manager.

```csharp
var privateKey = ...; // 32 bytes private key
var signer = new IdentitySchemeV4Signer(privateKey);
var verifier = new IdentitySchemeV4Verifier();
var sessionKeys = new SessionKeys(privateKey);
var sessionOptions = new SessionOptions.Builder()
  .WithSigner(signer)
  .WithVerifier(verifier)
  .WithSessionKeys(sessionKeys)
  .Build();
```

#### TableOptions Configuration

`TableOptions` allows you to configure the settings related with the node table manager.

```csharp
var tableOptions = new TableOptions.Builder()
  .WithRefreshIntervalMilliseconds(300000)
  .WithMaxAllowedFailures(5)
  .WithBootstrapEnrs(bootstrapEnrs)
  .Build();
```

#### Combining Options

After generating your custom configurations, you can provide them when creating `Discv5Protocol`. Here's how:

```csharp
var services = ServiceConfiguration.ConfigureServices(
  loggerFactory,
  connectionOptions,
  sessionOptions,
  tableOptions,
  talkResponder);

var serviceProvider = services.BuildServiceProvider();
var discv5 = new Discv5Protocol(serviceProvider);
```

Please note the customization and settings of mentioned options are optional and only recommended for advanced users who have a deep understanding of the underlying protocol, as they may affect the performance and functionality of the protocol.
