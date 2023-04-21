using Lantern.Discv5.WireProtocol.Utility;

namespace Lantern.Discv5.WireProtocol.Packets.Headers;

public class MaskedHeader
{
    private const int MaskingKeyLength = 16;
    
    public MaskedHeader(byte[] destNodeId, byte[]? maskingIv = default)
    {
        if (destNodeId.Length < MaskingKeyLength)
        {
            throw new ArgumentException($"destNodeId must be at least {MaskingKeyLength} bytes long.", nameof(destNodeId));
        }
        
        MaskingKey = destNodeId[..MaskingKeyLength];
        MaskingIv = maskingIv ?? new byte[MaskingKeyLength];
    }
    
    public byte[] MaskingKey { get; }
    
    public byte[] MaskingIv { get; }

    public byte[] GetMaskedHeader(byte[] header) => AesUtils.AesCtrEncrypt(MaskingKey, MaskingIv, header);
}