using Lantern.Discv5.Enr;
using Lantern.Discv5.Enr.Entries;
using Lantern.Discv5.WireProtocol.Connection;
using Lantern.Discv5.WireProtocol.Message;
using Lantern.Discv5.WireProtocol.Session;
using Lantern.Discv5.WireProtocol.Table;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;

namespace Lantern.Discv5.WireProtocol.Tests;

[TestFixture]
public class Discv5ProtocolBuilderTests
{
    
    [Test]
    public void WithSessionOptions_ChainsCorrectly_ReturnsBuilderInstance()
    {
        var builder = new Discv5ProtocolBuilder(new ServiceCollection());
        var returnedBuilder = builder.WithSessionOptions(new SessionOptions());

        Assert.AreSame(builder, returnedBuilder, "Method chaining should return the same builder instance.");
    }
    [Test]
    public void WithTableOptions_ChainsCorrectly_ReturnsBuilderInstance()
    {
        var builder = new Discv5ProtocolBuilder(new ServiceCollection());
        var returnedBuilder = builder.WithTableOptions(new TableOptions([]));
        
        Assert.AreSame(builder, returnedBuilder, "Method chaining should return the same builder instance.");
    }

    [Test]
    public void WithConnectionOptions_ChainsCorrectly_ReturnsBuilderInstance()
    {
        var builder = new Discv5ProtocolBuilder(new ServiceCollection());
        var returnedBuilder = builder.WithConnectionOptions(new ConnectionOptions());

        Assert.AreSame(builder, returnedBuilder, "Method chaining should return the same builder instance.");
    }

    [Test]
    public void WithConnectionOptions_ActionOverload_ChainsCorrectly_ReturnsBuilderInstance()
    {
        var builder = new Discv5ProtocolBuilder(new ServiceCollection());
        var returnedBuilder = builder.WithConnectionOptions(options => 
        {
            options.Port = 30303;
        });

        Assert.AreSame(builder, returnedBuilder, "Method chaining should return the same builder instance.");
    }

    [Test]
    public void WithSessionOptions_ActionOverload_ChainsCorrectly_ReturnsBuilderInstance()
    {
        var builder = new Discv5ProtocolBuilder(new ServiceCollection());
        var returnedBuilder = builder.WithSessionOptions(options =>
        {
            options.Verifier = SessionOptions.Default.Verifier;
        });

        Assert.AreSame(builder, returnedBuilder, "Method chaining should return the same builder instance.");
    }

    [Test]
    public void WithEnrBuilder_ChainsCorrectly_ReturnsBuilderInstance()
    {
        var builder = new Discv5ProtocolBuilder(new ServiceCollection());
        var enrBuilder = new EnrBuilder();
        var returnedBuilder = builder.WithEnrBuilder(enrBuilder);

        Assert.AreSame(builder, returnedBuilder, "Method chaining should return the same builder instance.");
    }

    [Test]
    public void WithEnrBuilder_ActionOverload_ChainsCorrectly_ReturnsBuilderInstance()
    {
        var builder = new Discv5ProtocolBuilder(new ServiceCollection());
        var returnedBuilder = builder.WithEnrBuilder(enrBuilder =>
        {
            enrBuilder.WithEntry(EnrEntryKey.Id, new EntryId("v4"));
        });

        Assert.AreSame(builder, returnedBuilder, "Method chaining should return the same builder instance.");
    }
    
    [Test]
    public void WithTableOptions_ActionOverload_ChainsCorrectly_ReturnsBuilderInstance()
    {
        var builder = new Discv5ProtocolBuilder(new ServiceCollection());
        var returnedBuilder = builder.WithTableOptions(options =>
        {
            options.BootstrapEnrs = ["enr:-example"];
        });

        Assert.AreSame(builder, returnedBuilder, "Method chaining should return the same builder instance.");
    }

    [Test]
    public void WithLoggerFactory_ChainsCorrectly_ReturnsBuilderInstance()
    {
        var builder = new Discv5ProtocolBuilder(new ServiceCollection());
        var loggerFactory = new LoggerFactory();
        var returnedBuilder = builder.WithLoggerFactory(loggerFactory);

        Assert.AreSame(builder, returnedBuilder, "Method chaining should return the same builder instance.");
    }

    [Test]
    public void WithEnrEntryRegistry_ChainsCorrectly_ReturnsBuilderInstance()
    {
        var builder = new Discv5ProtocolBuilder(new ServiceCollection());
        var entryRegistry = new EnrEntryRegistry();
        var returnedBuilder = builder.WithEnrEntryRegistry(entryRegistry);

        Assert.AreSame(builder, returnedBuilder, "Method chaining should return the same builder instance.");
    }

    [Test]
    public void WithTalkResponder_ChainsCorrectly_ReturnsBuilderInstance()
    {
        var builder = new Discv5ProtocolBuilder(new ServiceCollection());
        var talkResponder = Mock.Of<ITalkReqAndRespHandler>();
        var returnedBuilder = builder.WithTalkResponder(talkResponder);

        Assert.AreSame(builder, returnedBuilder, "Method chaining should return the same builder instance.");
    }

    [Test]
    public void Build_WithConfigurations_ReturnsConfiguredInstance()
    {
        string[] bootstrapEnrs = ["enr:-example"];
        var services = new ServiceCollection();
        var sessionOptions = SessionOptions.Default;
        var enr = new EnrBuilder()
            .WithIdentityScheme(sessionOptions.Verifier, sessionOptions.Signer)
            .WithEntry(EnrEntryKey.Id, new EntryId("v4"))
            .WithEntry(EnrEntryKey.Secp256K1, new EntrySecp256K1(sessionOptions.Signer.PublicKey));
        services.AddDiscv5(builder =>
        {
            builder.WithConnectionOptions(connectionOptions =>
            {
                connectionOptions.Port = 30303;
            }).WithTableOptions(new TableOptions(bootstrapEnrs))
                .WithLoggerFactory(NullLoggerFactory.Instance)
                .WithEnrBuilder(enr);
        });
        
        var serviceProvider = services.BuildServiceProvider();
        var discv5Protocol = serviceProvider.GetRequiredService<Discv5Protocol>();
        Assert.IsNotNull(discv5Protocol, "Expected to return a configured instance.");
    }

    [Test]
    public void Build_WithoutMandatoryConfigurations_ThrowsException()
    {
        var builder = new Discv5ProtocolBuilder(new ServiceCollection());
        Assert.Throws<InvalidOperationException>(() => builder.Build(), "Expected to throw due to missing configurations.");
    }
}