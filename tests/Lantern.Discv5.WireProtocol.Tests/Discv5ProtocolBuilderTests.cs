using System.Net;
using Lantern.Discv5.WireProtocol.Connection;
using Lantern.Discv5.WireProtocol.Session;
using Lantern.Discv5.WireProtocol.Table;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;

namespace Lantern.Discv5.WireProtocol.Tests;

[TestFixture]
public class Discv5ProtocolBuilderTests
{
    [Test]
    public void BuilderMethods_NullInputs_ThrowArgumentNullException() {
        var builder = new Discv5ProtocolBuilder();

        Assert.Throws<ArgumentNullException>(() => builder.WithConnectionOptions(null), "WithConnectionOptions should throw an ArgumentNullException when called with null.");
        Assert.Throws<ArgumentNullException>(() => builder.WithSessionOptions(null), "WithSessionOptions should throw an ArgumentNullException when called with null.");
        Assert.Throws<ArgumentNullException>(() => builder.WithTableOptions(null), "WithTableOptions should throw an ArgumentNullException when called with null.");
        Assert.Throws<ArgumentNullException>(() => builder.WithBootstrapEnrs(null), "WithBootstrapEnrs should throw an ArgumentNullException when called with null.");
        Assert.Throws<ArgumentNullException>(() => builder.WithEnrBuilder(null), "WithEnrBuilder should throw an ArgumentNullException when called with null.");
        Assert.Throws<ArgumentNullException>(() => builder.WithEnrEntryRegistry(null), "WithEnrEntryRegistry should throw an ArgumentNullException when called with null.");
        Assert.Throws<ArgumentNullException>(() => builder.WithTalkResponder(null), "WithTalkResponder should throw an ArgumentNullException when called with null.");
        Assert.Throws<ArgumentNullException>(() => builder.WithLoggerFactory(null), "WithLoggerFactory should throw an ArgumentNullException when called with null.");
    }

    [Test]
    public void WithSessionOptions_ChainsCorrectly_ReturnsBuilderInstance()
    {
        var builder = new Discv5ProtocolBuilder();
        var sessionOptions = new SessionOptions(); 
        var returnedBuilder = builder.WithSessionOptions(sessionOptions);
        
        Assert.AreSame(builder, returnedBuilder, "Method chaining should return the same builder instance.");
    }

    [Test]
    public void WithTableOptions_ChainsCorrectly_ReturnsBuilderInstance()
    {
        var builder = new Discv5ProtocolBuilder();
        var tableOptions = new TableOptions();
        var returnedBuilder = builder.WithTableOptions(tableOptions);
        
        Assert.AreSame(builder, returnedBuilder, "Method chaining should return the same builder instance.");
    }
    
    [Test]
    public void Build_WithConfigurations_ReturnsConfiguredInstance()
    {
        string[] bootstrapEnrs = ["enr:-example"];
        var connectionOptions = new ConnectionOptions
        {
            IpAddress = IPAddress.Loopback,
            Port = 30303
        };
        var builder = new Discv5ProtocolBuilder()
            .WithLoggerFactory(NullLoggerFactory.Instance)
            .WithConnectionOptions(connectionOptions)
            .WithBootstrapEnrs(bootstrapEnrs);
        
        var protocol = builder.Build();
        Assert.IsNotNull(protocol);
    }

    [Test]
    public void Build_WithoutMandatoryConfigurations_ThrowsException()
    {
        var builder = new Discv5ProtocolBuilder();
        Assert.Throws<InvalidOperationException>(() => builder.Build(), "Expected to throw due to missing configurations.");
    }
}