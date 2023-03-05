using Lantern.Discv5.Rlp;

namespace Lantern.Discv5.WireProtocol.Packets;

public class WhoAreYouPacket : Packet
{
    public WhoAreYouPacket(byte[] idNonce, ulong enrSeq) : base(PrepareAuthData(idNonce, enrSeq),
        AuthDataSizes.WhoAreYou)
    {
        IdNonce = idNonce;
        EnrSeq = enrSeq;
    }

    public byte[] IdNonce { get; }

    public ulong EnrSeq { get; }

    private static byte[] PrepareAuthData(byte[] idNonce, ulong enrSeq)
    {
        return Helpers.JoinMultipleByteArrays(idNonce, Helpers.ToBigEndianBytes(enrSeq));
    }
}