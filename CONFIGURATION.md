# Configuration Documentation

This section is dedicated for explaining the various configuration options for Lantern.Discv5.

## ConnectionOptions

`ConnectionOptions` allows you to adjust certain settings related to the connection manager. Below is an explanation of each option:

- `Port`: Specifies the port for the local node. Default value is `9000`.

- `IpAddress`: Indicates the IP address of the local machine on which the node is running.

- `ReceiveTimeoutMs`: Specifies the timeout in milliseconds for receiving a response to a request message. Default value is `1000` (1 second).

- `RequestTimeoutMs`: Specifies the timeout in milliseconds to remove pending messages for which a response has not been received. Default value is `2000` (2 seconds).

- `CheckPendingRequestsDelayMs`: Specifies the time interval in milliseconds to check for pending messages. Default value is `500` (0.5 seconds).

- `RemoveCompletedRequestsDelayMs`: Specifies the time interval in milliseconds for the removal of completed requests. Default value is `1000` (1 second).

## SessionOptions

`SessionOptions` allows you to adjust certain settings related to the session manager:

- `Signer`: The signer for the identity scheme (v4 as per the [ENR](https://github.com/ethereum/devp2p/blob/master/enr.md#v4-identity-scheme) Specification).

- `Verifier`: The verifier for the identity scheme (v4 as per the [ENR](https://github.com/ethereum/devp2p/blob/master/enr.md#v4-identity-scheme) Specification).

- `SessionKeys`: The private keys used for securing the session.

- `CacheSize`: Specifies the size of the cache for storing session objects. Default value is `1000`.

## TableOptions

`TableOptions` allows you to configure the settings related to the node table manager:

- `PingIntervalMilliseconds`: Specifies the time interval in milliseconds at which ping messages are sent to active peers. Default value is `5000` (5 seconds)

- `RefreshIntervalMilliseconds`: Specifies the time interval in milliseconds at which the node table is refreshed by performing a lookup to ensure that the knowledge of active peers is up-to-date. Default value is `300000` (5 minutes)

- `LookupTimeoutMilliseconds`: Specifies the maximum time in milliseconds to perform a lookup operation. Default value is `60000` (1 minute)

- `ConcurrencyParameter`: Specifies the concurrency factor (k) for the Kademlia-style lookups and routing. This is the number of nodes that are queried each `FINDNODE` request during the lookup. Default value is `3`

- `LookupParallelism`: Specifies the maximum number of lookup operations (path buckets) that can be performed in parallel while maintaining the concurrency parameter (k). Default value is `2`.

- `MaxAllowedFailures`: Specifies the maximum number of allowed unsuccessful interaction attempts with a peer before it is considered as non-responsive and added to the blacklisting mechanism. Default value is `3`

- `BootstrapEnrs`: Specifies an array of Ethereum Node Records (ENR) for bootstrap peers.

In each of these options, the builder pattern is used for creating the desired configuration. Each builder class provides various `With` methods that you can use to customize the configuration according to your requirement. The option configuration will retain the default values for options that you do not set using these `With` methods. After finishing the customization, you can finalize your configuration by calling the `Build` method.
