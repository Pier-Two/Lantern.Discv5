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

    public byte[] GetMaskedHeader(byte[] header) => AesCryptography.AesCtrEncrypt(MaskingKey, MaskingIv, header);
}