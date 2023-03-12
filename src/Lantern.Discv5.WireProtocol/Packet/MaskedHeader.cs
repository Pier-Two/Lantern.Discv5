using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;

namespace Lantern.Discv5.WireProtocol.Packet;

public class MaskedHeader
{
    public MaskedHeader(byte[] destinationNodeId, byte[]? maskingIv = default)
    {
        MaskingKey = destinationNodeId[..16];
        MaskingIv = maskingIv ?? new byte[16];
    }
    
    public byte[] MaskingKey { get; }
    
    public byte[] MaskingIv { get; }

    public byte[] GetMaskedHeader(byte[] header)
    {
        var cipher = CipherUtilities.GetCipher("AES/CTR/NoPadding");
        var keyParam = new KeyParameter(MaskingKey);
        var parameters = new ParametersWithIV(keyParam, MaskingIv);

        cipher.Init(true, parameters);

        var cipherText = new byte[cipher.GetOutputSize(header.Length)];
        var len = cipher.ProcessBytes(header, 0, header.Length, cipherText, 0);
        cipher.DoFinal(cipherText, len);

        return cipherText;
    }
}