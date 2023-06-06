using Lantern.Discv5.WireProtocol.Connection;
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
    private ILoggerFactory _loggerFactory;
    
    public Discv5Builder WithConnectionOptions(ConnectionOptions connectionOptions)
    {
        _connectionOptions = connectionOptions;
        return this;
    }

    public Discv5Builder WithSessionOptions(SessionOptions sessionOptions)
    {
        _sessionOptions = sessionOptions;
        return this;
    }

    public Discv5Builder WithTableOptions(TableOptions tableOptions)
    {
        _tableOptions = tableOptions;
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
            _loggerFactory, _connectionOptions, _sessionOptions, _tableOptions
        );
        var serviceProvider = services.BuildServiceProvider();

        return new Discv5Protocol(serviceProvider);
    }
}