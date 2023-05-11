using System.Security.Cryptography;

namespace Lantern.Discv5.WireProtocol.Utility;

public static class RandomUtility
{
    public static byte[] GeneratePrivateKey(int privateKeyLength)
    {
        return GenerateRandomBytes(privateKeyLength);
    }

    public static byte[] GenerateNodeId(int nodeIdLength)
    {
        return GenerateRandomBytes(nodeIdLength);
    }
    
    public static byte[] GenerateIdNonce(int idNonceLength)
    {
        return GenerateRandomBytes(idNonceLength);
    }
    
    public static byte[] GenerateNonce(int nonceLength)
    {
        return GenerateRandomBytes(nonceLength);
    }

    public static byte[] GenerateMaskingIv(int maskingIvLength)
    {
        return GenerateRandomBytes(maskingIvLength);
    }

    public static byte[] GenerateRandomData(int randomDataLength)
    {
        return GenerateRandomBytes(randomDataLength);
    }

    private static byte[] GenerateRandomBytes(int size)
    {
        using var random = RandomNumberGenerator.Create();
        
        var bytes = new byte[size];
        random.GetBytes(bytes);
        
        return bytes;
    }
}