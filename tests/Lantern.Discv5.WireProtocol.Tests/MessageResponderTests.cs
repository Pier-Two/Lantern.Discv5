using Lantern.Discv5.WireProtocol.Connection;
using Lantern.Discv5.WireProtocol.Identity;
using Lantern.Discv5.WireProtocol.Logging;
using Lantern.Discv5.WireProtocol.Message;
using Lantern.Discv5.WireProtocol.Packet;
using Lantern.Discv5.WireProtocol.Packet.Handlers;
using Lantern.Discv5.WireProtocol.Session;
using Lantern.Discv5.WireProtocol.Table;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

namespace Lantern.Discv5.WireProtocol.Tests;

[TestFixture]
public class MessageResponderTests
{
    private static IMessageResponder _messageResponder = null!;

    [SetUp]
    public void Setup()
    {
        var connectionOptions = ConnectionOptions.Default;
        var sessionOptions = SessionOptions.Default;
        var tableOptions = TableOptions.Default;
        var loggerFactory = LoggingOptions.Default;
        var serviceProvider =
            ServiceConfiguration.ConfigureServices(loggerFactory, connectionOptions, sessionOptions, tableOptions).BuildServiceProvider();
        
        _messageResponder = serviceProvider.GetRequiredService<IMessageResponder>();
    }

    [Test]
    public void Test_MessageResponder_ShouldHandlePingMessageCorrectly()
    {
        //TODO
    }
    
}