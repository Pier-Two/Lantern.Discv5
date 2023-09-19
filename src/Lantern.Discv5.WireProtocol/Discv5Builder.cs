using Lantern.Discv5.Enr;
using Lantern.Discv5.Enr.Entries;
using Lantern.Discv5.Enr.Identity;
using Lantern.Discv5.WireProtocol.Connection;
using Lantern.Discv5.WireProtocol.Logging;
using Lantern.Discv5.WireProtocol.Message;
using Lantern.Discv5.WireProtocol.Services;
using Lantern.Discv5.WireProtocol.Session;
using Lantern.Discv5.WireProtocol.Table;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lantern.Discv5.WireProtocol;

public class Discv5Builder
{
    private ConnectionOptions _connectionOptions = ConnectionOptions.Default;
    private SessionOptions _sessionOptions = SessionOptions.Default;
    private IEnrEntryRegistry _entryRegistry = EnrEntryRegistry.Default;
    private ILoggerFactory _loggerFactory = LoggingOptions.Default;
    private TableOptions _tableOptions;
    private string[] _bootstrapEnrs;
    private EnrBuilder _enrBuilder;
    private Enr.Enr _enr;
    private ITalkReqAndRespHandler _talkResponder;
        
    public Discv5Builder WithConnectionOptions(ConnectionOptions options)
    {
        _connectionOptions = options ?? throw new ArgumentNullException(nameof(options));
        return this;
    }
        
    public Discv5Builder WithSessionOptions(SessionOptions options)
    {
        _sessionOptions = options ?? throw new ArgumentNullException(nameof(options));
        return this;
    }
        
    public Discv5Builder WithTableOptions(TableOptions options)
    {
        _tableOptions = options ?? throw new ArgumentNullException(nameof(options));
        return this;
    }
        
    public Discv5Builder WithBootstrapEnrs(string[] bootstrapEnrs)
    {
        _bootstrapEnrs = bootstrapEnrs ?? throw new ArgumentNullException(nameof(bootstrapEnrs));
        return this;
    }

    public Discv5Builder WithEnrBuilder(EnrBuilder enrBuilder)
    {
        _enrBuilder = enrBuilder ?? throw new ArgumentNullException(nameof(enrBuilder));
        return this;
    }
        
    public Discv5Builder WithEnrEntryRegistry(IEnrEntryRegistry enrEntryRegistry)
    {
        _entryRegistry = enrEntryRegistry ?? throw new ArgumentNullException(nameof(enrEntryRegistry));
        return this;
    }
        
    public Discv5Builder WithTalkResponder(ITalkReqAndRespHandler talkResponder)
    {
        _talkResponder = talkResponder ?? throw new ArgumentNullException(nameof(talkResponder));
        return this;
    }

    public Discv5Builder WithLoggerFactory(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        return this;
    }

    public Discv5Protocol Build()
    {
        _tableOptions ??= GetDefaultTableOptions();
        _enrBuilder ??= GetDefaultEnrBuilder();

        var services = ServiceConfiguration.ConfigureServices(
            _loggerFactory, 
            _connectionOptions, 
            _sessionOptions, 
            _entryRegistry,
            _enrBuilder.Build(), 
            _tableOptions, 
            _talkResponder
        );

        var serviceProvider = services.BuildServiceProvider();
        
        return serviceProvider.GetRequiredService<Discv5Protocol>();
    }
    
    public static Discv5Protocol CreateDefault(string[] bootstrapEnrs)
    {
        return new Discv5Builder()
            .WithBootstrapEnrs(bootstrapEnrs)
            .Build();
    }
        
    public static Enr.Enr CreateNewRecord(ConnectionOptions options, IIdentityVerifier verifier, IIdentitySigner signer)
    {
        var builder = new EnrBuilder()
            .WithIdentityScheme(verifier, signer)
            .WithEntry(EnrEntryKey.Id, new EntryId("v4"))
            .WithEntry(EnrEntryKey.Secp256K1, new EntrySecp256K1(signer.PublicKey));

        if (options.IpAddress != null)
        {
            builder.WithEntry(EnrEntryKey.Ip, new EntryIp(options.IpAddress)) 
                .WithEntry(EnrEntryKey.Udp, new EntryUdp(options.Port));
        }

        return builder.Build();
    }
        
    private EnrBuilder GetDefaultEnrBuilder()
    {
        return new EnrBuilder()
            .WithIdentityScheme(_sessionOptions.Verifier, _sessionOptions.Signer)
            .WithEntry(EnrEntryKey.Id, new EntryId("v4"))
            .WithEntry(EnrEntryKey.Secp256K1, new EntrySecp256K1(_sessionOptions.Signer.PublicKey));
    }
    
    private TableOptions GetDefaultTableOptions()
    {
        return new TableOptions().SetBootstrapEnrs(_bootstrapEnrs);
    }
}