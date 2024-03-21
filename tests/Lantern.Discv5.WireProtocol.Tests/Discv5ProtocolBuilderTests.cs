using System.Net;
using Lantern.Discv5.WireProtocol.Connection;
using Lantern.Discv5.WireProtocol.Session;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;

namespace Lantern.Discv5.WireProtocol.Tests;

[TestFixture]
public class Discv5ProtocolBuilderTests
{
   [Test]
    public void WithConnectionOptions_NullInput_ThrowsArgumentNullException()
    {
        var builder = new Discv5ProtocolBuilder();
        Assert.Throws<ArgumentNullException>(() => builder.WithConnectionOptions(null));
    }

    [Test]
    public void WithEnrBuilder_NullInput_ThrowsArgumentNullException()
    {
        var builder = new Discv5ProtocolBuilder();
        Assert.Throws<ArgumentNullException>(() => builder.WithEnrBuilder(null));
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