using System.Security.Cryptography;

namespace Lantern.Discv5.WireProtocol.Packet;

public class EncryptedMessage
{
    public EncryptedMessage(byte[] messagePt, byte[] messageAd)
    {
        MessagePt = messagePt;
        MessageAd = messageAd;
    }

    public readonly byte[] MessagePt;

    public readonly byte[] MessageAd;

    public byte[] AesGcmEncrypt(byte[] initiatorKey, byte[] nonce)
    {
        using var aesGcm = new AesGcm(initiatorKey);
        var ciphertext = new byte[MessagePt.Length];
        aesGcm.Encrypt(nonce, MessagePt, ciphertext, MessageAd);
        return ciphertext;
    }
}