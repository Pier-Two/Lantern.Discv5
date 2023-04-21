using Lantern.Discv5.Rlp;
using Lantern.Discv5.WireProtocol.Packets.Constants;

namespace Lantern.Discv5.WireProtocol.Packets.Types;

public class WhoAreYouPacket : Packet
{
    public WhoAreYouPacket(byte[] idNonce, ulong enrSeq) : base(PreparePacketBase(idNonce, enrSeq))
    {
        IdNonce = idNonce;
        EnrSeq = enrSeq;
    }

    public readonly byte[] IdNonce;

    public readonly ulong EnrSeq;

    private static byte[] PreparePacketBase(byte[] idNonce, ulong enrSeq)
    {
        var enrSeqArray = new byte[AuthDataSizes.EnrSeqSize];
        var enrSeqBytes = ByteArrayUtils.ToBigEndianBytesTrimmed(enrSeq);
        Array.Copy(enrSeqBytes, 0, enrSeqArray, AuthDataSizes.EnrSeqSize - enrSeqBytes.Length, enrSeqBytes.Length);
        return ByteArrayUtils.JoinByteArrays(idNonce, enrSeqArray);
    }

    public static WhoAreYouPacket DecodeAuthData(byte[] authData)
    {
        var index = 0;
        var idNonce = authData[..AuthDataSizes.IdNonceSize];
        index += AuthDataSizes.IdNonceSize;
        
        var enrSeq = (ulong)RlpExtensions.ByteArrayToInt64(authData[index..AuthDataSizes.WhoAreYou]);
        return new WhoAreYouPacket(idNonce, enrSeq);
    }
}