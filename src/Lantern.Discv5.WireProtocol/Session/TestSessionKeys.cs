namespace Lantern.Discv5.WireProtocol.Session;

[Obsolete("This class is only used for testing purposes.")]
public class TestSessionKeys : BaseSessionKeys
{
    public TestSessionKeys(byte[]? privateKey = null, byte[]? ephemeralPrivateKey = null)
    {
        PrivateKey = privateKey != null ? CryptoContext.CreateECPrivKey(privateKey) : GeneratePrivateKey();
        EphemeralPrivateKey = ephemeralPrivateKey != null ? CryptoContext.CreateECPrivKey(ephemeralPrivateKey) : GeneratePrivateKey();
    }
}