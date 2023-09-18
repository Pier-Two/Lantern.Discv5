using Lantern.Discv5.Enr;
using Lantern.Discv5.Enr.Entries;
using Lantern.Discv5.Enr.Identity;
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
    private IEnrEntryRegistry _entryRegistry;
    private Enr.Enr _enr;
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
        _enr = enrBuilder.Build();
        return this;
    }
    
    public Discv5Builder WithEnrEntryRegistry(EnrEntryRegistry enrEntryRegistry)
    {
        _entryRegistry = enrEntryRegistry;
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
            _entryRegistry,
            _enr, 
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
            .WithEnrEntryRegistry(new EnrEntryRegistry())
            .WithEnrBuilder(new EnrBuilder()
                .WithIdentityScheme(sessionOptions.Verifier, sessionOptions.Signer)
                .WithEntry(EnrEntryKey.Id, new EntryId("v4"))
                .WithEntry(EnrEntryKey.Secp256K1, new EntrySecp256K1(sessionOptions.Signer.PublicKey)))
            .WithLoggerFactory(loggerFactory)
            .WithTalkResponder(talkResponder)
            .Build();
        return discv5;
    }
    
    public static Enr.Enr CreateNewRecord(ConnectionOptions options, IIdentityVerifier verifier, IIdentitySigner signer)
    {
        Enr.Enr record;
        
        if (options.IpAddress != null)
        {
            record = new EnrBuilder()
                .WithIdentityScheme(verifier, signer)
                .WithEntry(EnrEntryKey.Id, new EntryId("v4")) // Replace with a constant
                .WithEntry(EnrEntryKey.Ip, new EntryIp(options.IpAddress)) 
                .WithEntry(EnrEntryKey.Udp, new EntryUdp(options.Port))
                .WithEntry(EnrEntryKey.Secp256K1, new EntrySecp256K1(signer.PublicKey))
                .Build();
        }
        else
        {
            record = new EnrBuilder()
                .WithIdentityScheme(verifier, signer)
                .WithEntry(EnrEntryKey.Id, new EntryId("v4")) // Replace with a constant
                .WithEntry(EnrEntryKey.Secp256K1, new EntrySecp256K1(signer.PublicKey))
                .Build();
        }
        
        return record;
    }
}