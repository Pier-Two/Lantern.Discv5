using System.Security.Cryptography;

namespace Lantern.Discv5.WireProtocol.Utility;

public static class PacketUtils
{
    private const int IdNonceSize = 16;
    private const int MaskingIvSize = 16;
    private const int NonceSize = 12;
    
    public static byte[] GenerateIdNonce()
    {
        using var random = RandomNumberGenerator.Create();
        var idNonce = new byte[IdNonceSize];
        random.GetBytes(idNonce);
        return idNonce;
    }
    
    public static byte[] GenerateNonce()
    {
        using var random = RandomNumberGenerator.Create();
        var nonce = new byte[NonceSize];
        random.GetBytes(nonce);
        return nonce;
    }

    public static byte[] GenerateMaskingIv()
    {
        using var random = RandomNumberGenerator.Create();
        var maskingIv = new byte[MaskingIvSize];
        random.GetBytes(maskingIv);
        return maskingIv;
    }
    
    
}