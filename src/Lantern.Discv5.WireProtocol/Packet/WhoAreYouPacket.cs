using Lantern.Discv5.Rlp;

namespace Lantern.Discv5.WireProtocol.Packet;

public class WhoAreYouPacket : PacketBase
{
    public WhoAreYouPacket(byte[] idNonce, int enrSeq) : base(PreparePacketBase(idNonce, enrSeq))
    {
        IdNonce = idNonce;
        EnrSeq = enrSeq;
    }

    public readonly byte[] IdNonce;

    public readonly int EnrSeq;

    private static byte[] PreparePacketBase(byte[] idNonce, int enrSeq)
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
        
        var enrSeq = (int)RlpExtensions.ByteArrayToInt64(authData[index..AuthDataSizes.WhoAreYou]);
        return new WhoAreYouPacket(idNonce, enrSeq);
    }
}