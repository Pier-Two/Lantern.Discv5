using Lantern.Discv5.Enr;
using Lantern.Discv5.Enr.EnrContent;
using Lantern.Discv5.Enr.EnrContent.Entries;
using Lantern.Discv5.Enr.IdentityScheme.Interfaces;
using Lantern.Discv5.WireProtocol.Connection;
using Lantern.Discv5.WireProtocol.Logging;
using Lantern.Discv5.WireProtocol.Message;
using Lantern.Discv5.WireProtocol.Session;
using Lantern.Discv5.WireProtocol.Table;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lantern.Discv5.WireProtocol;

public class Discv5Builder
{
    private ConnectionOptions _connectionOptions;
    private SessionOptions _sessionOptions;
    private TableOptions _tableOptions;
    private string[] _bootstrapEnrs;
    private EnrRecord _enrRecord;
    private ITalkReqAndRespHandler _talkResponder;
    private ILoggerFactory _loggerFactory = LoggingOptions.Default;
    
    public Discv5Builder WithConnectionOptions(ConnectionOptions options)
    {
        _connectionOptions = options;
        return this;
    }
    
    public Discv5Builder WithSessionOptions(SessionOptions options)
    {
        _sessionOptions = options;
        return this;
    }
    
    public Discv5Builder WithTableOptions(TableOptions options)
    {
        _tableOptions = options;
        return this;
    }
    
    public Discv5Builder WithBootstrapEnrs(string[] bootstrapEnrs)
    {
        _bootstrapEnrs = bootstrapEnrs;
        return this;
    }

    public Discv5Builder WithEnrBuilder(EnrBuilder enrBuilder)
    {
        _enrRecord = enrBuilder.Build();
        return this;
    }
    
    public Discv5Builder WithTalkResponder(ITalkReqAndRespHandler talkResponder)
    {
        _talkResponder = talkResponder;
        return this;
    }

    public Discv5Builder WithLoggerFactory(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory;
        return this;
    }

    public Discv5Protocol Build()
    {
        var services = ServiceConfiguration.ConfigureServices(
            _loggerFactory, 
            _connectionOptions, 
            _sessionOptions, 
            _enrRecord, 
            _tableOptions, 
            _talkResponder
        );

        var serviceProvider = services.BuildServiceProvider();
    
        return serviceProvider.GetRequiredService<Discv5Protocol>();
    }
    
    public static Discv5Protocol CreateDefault(string[] bootstrapEnrs, ITalkReqAndRespHandler? talkResponder = null)
    {
        var connectionOptions = ConnectionOptions.Default;
        var sessionOptions = SessionOptions.Default;
        var tableOptions = new TableOptions.Builder()
            .WithBootstrapEnrs(bootstrapEnrs)
            .Build();
        
        var loggerFactory = LoggingOptions.Default;
        var discv5 = new Discv5Builder()
            .WithConnectionOptions(connectionOptions)
            .WithSessionOptions(sessionOptions)
            .WithTableOptions(tableOptions)
            .WithBootstrapEnrs(bootstrapEnrs)
            .WithEnrBuilder(new EnrBuilder()
                .WithIdentityScheme(sessionOptions.Verifier, sessionOptions.Signer)
                .WithEntry(EnrContentKey.Id, new EntryId("v4"))
                .WithEntry(EnrContentKey.Secp256K1, new EntrySecp256K1(sessionOptions.Signer.PublicKey)))
            .WithLoggerFactory(loggerFactory)
            .WithTalkResponder(talkResponder)
            .Build();
        return discv5;
    }
    
    public static EnrRecord CreateNewRecord(ConnectionOptions options, IIdentitySchemeVerifier verifier, IIdentitySchemeSigner signer)
    {
        EnrRecord record;
        
        if (options.IpAddress != null)
        {
            record = new EnrBuilder()
                .WithIdentityScheme(verifier, signer)
                .WithEntry(EnrContentKey.Id, new EntryId("v4")) // Replace with a constant
                .WithEntry(EnrContentKey.Ip, new EntryIp(options.IpAddress)) 
                .WithEntry(EnrContentKey.Udp, new EntryUdp(options.Port))
                .WithEntry(EnrContentKey.Secp256K1, new EntrySecp256K1(signer.PublicKey))
                .Build();
        }
        else
        {
            record = new EnrBuilder()
                .WithIdentityScheme(verifier, signer)
                .WithEntry(EnrContentKey.Id, new EntryId("v4")) // Replace with a constant
                .WithEntry(EnrContentKey.Secp256K1, new EntrySecp256K1(signer.PublicKey))
                .Build();
        }
        
        return record;
    }
}