using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;

namespace Lantern.Discv5.WireProtocol.Packet;

public static class AesCryptography
{
    public static byte[] AesCtrEncrypt(byte[] maskingKey, byte[] maskingIv, byte[] header)
    {
        var cipher = CipherUtilities.GetCipher("AES/CTR/NoPadding");
        var keyParam = new KeyParameter(maskingKey);
        var parameters = new ParametersWithIV(keyParam, maskingIv);

        cipher.Init(true, parameters);

        var cipherText = new byte[cipher.GetOutputSize(header.Length)];
        var len = cipher.ProcessBytes(header, 0, header.Length, cipherText, 0);
        cipher.DoFinal(cipherText, len);

        return cipherText;
    }

    public static byte[] AesCtrDecrypt(byte[] destId, byte[] maskedHeader)
    {
        var maskingIv = new byte[16];
        var encryptedHeader = new byte[maskedHeader.Length - 16];
        Array.Copy(maskedHeader, 0, maskingIv, 0, 16);
        Array.Copy(maskedHeader, 16, encryptedHeader, 0, encryptedHeader.Length);

        var maskingKey = new byte[16];
        Array.Copy(destId, 0, maskingKey, 0, 16);

        var cipher = CipherUtilities.GetCipher("AES/CTR/NoPadding");
        cipher.Init(false, new ParametersWithIV(new KeyParameter(maskingKey), maskingIv));
        
        return cipher.DoFinal(encryptedHeader);
    }

    public static byte[] AesGcmEncrypt(byte[] key, byte[] nonce, byte[] plaintext, byte[] ad)
    {
        var cipher = new GcmBlockCipher(new AesEngine());
        var parameters = new AeadParameters(new KeyParameter(key), 128, nonce, ad);
        
        cipher.Init(true, parameters);
        
        var ciphertext = new byte[cipher.GetOutputSize(plaintext.Length)];
        var len = cipher.ProcessBytes(plaintext, 0, plaintext.Length, ciphertext, 0);
        
        cipher.DoFinal(ciphertext, len);

        return ciphertext;
    }
    
    public static byte[] AesGcmDecrypt(byte[] key, byte[] nonce, byte[] ciphertext, byte[] ad)
    {
        var cipher = new GcmBlockCipher(new AesEngine());
        var parameters = new AeadParameters(new KeyParameter(key), 128, nonce, ad);
        
        cipher.Init(false, parameters);
        
        var plaintext = new byte[cipher.GetOutputSize(ciphertext.Length)];
        var len = cipher.ProcessBytes(ciphertext, 0, ciphertext.Length, plaintext, 0); 
        cipher.DoFinal(plaintext, len);

        return plaintext;
    }
}