using Lantern.Discv5.Rlp;

namespace Lantern.Discv5.WireProtocol.Packets;

public class HandshakePacket : Packet
{
    public HandshakePacket(byte[] idSignature, byte[] ephPubkey, byte[] record, byte[] srcId, int sigSize,
        int ephKeySize) : base(PrepareAuthData(idSignature, ephPubkey, record, srcId, sigSize, ephKeySize),
        AuthDataSizes.Handshake + sigSize + ephKeySize + record.Length)
    {
        IdSignature = idSignature;
        EphPubkey = ephPubkey;
        Record = record;
        SrcId = srcId;
        SigSize = sigSize;
        EphKeySize = ephKeySize;
    }

    public byte[] IdSignature { get; }

    public byte[] EphPubkey { get; }

    public byte[] Record { get; }

    public byte[] SrcId { get; }

    public int SigSize { get; }

    public int EphKeySize { get; }

    private static byte[] PrepareAuthData(byte[] idSignature, byte[] ephPubkey, byte[] record, byte[] srcId,
        int sigSize, int ephKeySize)
    {
        return Helpers.JoinMultipleByteArrays(srcId, Helpers.ToBigEndianBytes(sigSize),
            Helpers.ToBigEndianBytes(ephKeySize), idSignature, ephPubkey, record);
    }
}