using System.Security.Cryptography;

namespace Lantern.Discv5.WireProtocol;

public static class RandomUtility
{
    public static byte[] GetRandomNonce()
    {
        var nonce = new byte[12];
        RandomNumberGenerator.Create().GetBytes(nonce);
        return nonce;
    }
}