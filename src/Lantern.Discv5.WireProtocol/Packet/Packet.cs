namespace Lantern.Discv5.WireProtocol.Packet;

public class Packet
{
    public Packet(byte[] maskingIv, byte[] maskedHeader, byte[]? message = default)
    {
        MaskingIv = maskingIv;
        MaskedHeader = maskedHeader;
        Message = message;
    }

    public readonly byte[] MaskingIv;

    public readonly byte[] MaskedHeader;

    public readonly byte[]? Message;
}