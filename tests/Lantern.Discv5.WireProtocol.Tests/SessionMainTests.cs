using Lantern.Discv5.WireProtocol.Connection;
using Lantern.Discv5.WireProtocol.Identity;
using Lantern.Discv5.WireProtocol.Logging;
using Lantern.Discv5.WireProtocol.Packet;
using Lantern.Discv5.WireProtocol.Packet.Types;
using Lantern.Discv5.WireProtocol.Session;
using Lantern.Discv5.WireProtocol.Table;
using Lantern.Discv5.WireProtocol.Utility;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

namespace Lantern.Discv5.WireProtocol.Tests;

[TestFixture]
public class SessionMainTests
{
    private static ISessionMain _sessionMain = null!;

    [SetUp]
    public void Setup()
    {
        var connectionOptions = new ConnectionOptions.Builder()
            .WithPort(2040)
            .Build();
        
        var tableOptions = TableOptions.Default;
        var sessionOptions = SessionOptions.Default;
        var loggerFactory = LoggingOptions.Default;
        
        var serviceProvider = ServiceConfiguration.ConfigureServices(loggerFactory, connectionOptions, sessionOptions, tableOptions).BuildServiceProvider();
        var aesUtility = serviceProvider.GetRequiredService<IAesCrypto>();
        var logger = serviceProvider.GetRequiredService<ILoggerFactory>();
        var sessionCrypto = serviceProvider.GetRequiredService<ISessionCrypto>();
        
        _sessionMain = new SessionMain(sessionOptions.SessionKeys, aesUtility, sessionCrypto, logger,SessionType.Initiator);
    }

    [Test]
    public void Test_SessionMain_ShouldReturnNullWhenChallengeDataIsNotSet()
    {
        var result = _sessionMain.GenerateIdSignature(RandomUtility.GenerateRandomData(32));
        Assert.Null(result);
    }
    
    [Test]
    public void Test_SessionMain_ShouldNotVerifyWhenChallengeDataIsNotValid()
    {
        var handshake = new HandshakePacketBase(RandomUtility.GenerateRandomData(32),
            RandomUtility.GenerateRandomData(32), RandomUtility.GenerateRandomData(32));
        var result = _sessionMain.VerifyIdSignature(handshake, RandomUtility.GenerateRandomData(32), RandomUtility.GenerateRandomData(32));
        Assert.IsFalse(result);
    }

    /*
    [Test]
    public void Test_SessionMain_ShouldReturnNullWhenCurrentSharedKeysAreNotSet()
    {
        var packetProcessor = new PacketProcessor(_identityManager, _aesUtility, RandomUtility.GenerateRandomData(32));
        var result = _sessionMain.DecryptMessage(packetProcessor);
        Assert.Null(result);
    }*/
}