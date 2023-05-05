using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;

namespace Lantern.Discv5.WireProtocol.Session;

/// <summary>
/// Provides utility methods for handling AES cryptography operations.
/// </summary>
public static class AESUtility
{
    private const int AesBlockSize = 16;
    private const int GcmTagSize = 128;
    
    public static byte[] AesCtrEncrypt(byte[] maskingKey, byte[] maskingIv, byte[] header)
    {
        if (maskingKey.Length != AesBlockSize || maskingIv.Length != AesBlockSize)
        {
            throw new ArgumentException("Invalid key or IV length.");
        }

        var cipher = CipherUtilities.GetCipher("AES/CTR/NoPadding");
        var keyParam = new KeyParameter(maskingKey);
        var parameters = new ParametersWithIV(keyParam, maskingIv);

        cipher.Init(true, parameters);

        var cipherText = new byte[cipher.GetOutputSize(header.Length)];
        var len = cipher.ProcessBytes(header, 0, header.Length, cipherText, 0);
        cipher.DoFinal(cipherText, len);

        return cipherText;
    }

    public static byte[] AesCtrDecrypt(byte[] maskingKey, byte[] maskingIv, byte[] maskedHeader)
    {
        if (maskingKey.Length != AesBlockSize || maskingIv.Length != AesBlockSize)
        {
            throw new ArgumentException("Invalid key or IV length.");
        }
        
        var cipher = CipherUtilities.GetCipher("AES/CTR/NoPadding");
        cipher.Init(false, new ParametersWithIV(new KeyParameter(maskingKey), maskingIv));

        return cipher.DoFinal(maskedHeader);
    }
    
    public static byte[] AesGcmEncrypt(byte[] key, byte[] nonce, byte[] plaintext, byte[] ad)
    {
        var cipher = new GcmBlockCipher(new AesEngine());
        var parameters = new AeadParameters(new KeyParameter(key), GcmTagSize, nonce, ad);

        cipher.Init(true, parameters);

        var ciphertext = new byte[cipher.GetOutputSize(plaintext.Length)];
        var len = cipher.ProcessBytes(plaintext, 0, plaintext.Length, ciphertext, 0);

        cipher.DoFinal(ciphertext, len);

        return ciphertext;
    }
    
    public static byte[] AesGcmDecrypt(byte[] key, byte[] nonce, byte[] ciphertext, byte[] ad)
    {
        var cipher = new GcmBlockCipher(new AesEngine());
        var parameters = new AeadParameters(new KeyParameter(key), GcmTagSize, nonce, ad);

        cipher.Init(false, parameters);

        var plaintext = new byte[cipher.GetOutputSize(ciphertext.Length)];
        var len = cipher.ProcessBytes(ciphertext, 0, ciphertext.Length, plaintext, 0);
        cipher.DoFinal(plaintext, len);

        return plaintext;
    }
}