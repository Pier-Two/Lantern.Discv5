using Lantern.Discv5.Enr;
using Lantern.Discv5.Enr.EnrFactory;
using Lantern.Discv5.Enr.IdentityScheme.V4;
using Lantern.Discv5.WireProtocol.Connection;
using Lantern.Discv5.WireProtocol.Session;
using Lantern.Discv5.WireProtocol.Table;
using Lantern.Discv5.WireProtocol.Utility;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

namespace Lantern.Discv5.WireProtocol;

public class Discv5Factory
{
    public static Discv5Protocol CreateDiscv5(string[] bootstrapEnrs)
    {
        var privateKey = RandomUtility.GeneratePrivateKey(32);
        var signer = new IdentitySchemeV4Signer(privateKey);
        var verifier = new IdentitySchemeV4Verifier();
        var sessionKeys = new SessionKeys(privateKey);
        var bootstrapEnrRecords = GetBootstrapEnrRecords(bootstrapEnrs);
        
        var loggerFactory = CreateLoggerFactory();
        
        var connectionOptions = new ConnectionOptions.Builder()
            .WithExternalIpAddressAsync().Result
            .Build();

        var sessionOptions = new SessionOptions.Builder()
            .WithSigner(signer)
            .WithVerifier(verifier)
            .WithSessionKeys(sessionKeys)
            .WithCacheSize(100)
            .Build();

        var tableOptions = new TableOptions.Builder()
            .WithBootstrapEnrs(bootstrapEnrRecords)
            .Build();

        return new Discv5Builder()
            .WithConnectionOptions(connectionOptions)
            .WithSessionOptions(sessionOptions)
            .WithTableOptions(tableOptions)
            .WithLoggerFactory(loggerFactory)
            .Build();
    }

    private static EnrRecord[] GetBootstrapEnrRecords(string[] bootstrapEnrs)
    {
        return bootstrapEnrs
            .Select(enr => new EnrRecordFactory().CreateFromString(enr))
            .ToArray();
    }

    private static ILoggerFactory CreateLoggerFactory()
    {
        return LoggerFactory.Create(builder =>
        {
            builder
                .AddFilter("Microsoft", LogLevel.Warning)
                .AddFilter("System", LogLevel.Warning)
                .AddSimpleConsole(options =>
                {
                    options.ColorBehavior = LoggerColorBehavior.Enabled;
                    options.IncludeScopes = false;
                    options.SingleLine = true;
                    options.TimestampFormat = "[HH:mm:ss] ";
                    options.UseUtcTimestamp = true;
                });
        });
    }
}