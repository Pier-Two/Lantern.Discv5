using Lantern.Discv5.Rlp;

namespace Lantern.Discv5.WireProtocol.Packet.Types;

public class HandshakePacket : Packet
{
    public HandshakePacket(byte[] idSignature, byte[] ephPubkey, byte[] srcId, byte[]? record = null) : base(PreparePacketBase(idSignature, ephPubkey, srcId, record))
    {
        IdSignature = idSignature;
        EphPubkey = ephPubkey;
        SrcId = srcId;
        Record = record;
    }
    
    public byte[] IdSignature { get; }
    
    public byte[] EphPubkey { get; }
    
    public byte[]? SrcId { get; }
    
    public byte[]? Record { get; }

    private static byte[] PreparePacketBase(byte[] idSignature, byte[] ephPubkey, byte[] srcId, byte[]? record = null)
    {
        var authDataHead = ByteArrayUtils.Concatenate(srcId, ByteArrayUtils.ToBigEndianBytesTrimmed(idSignature.Length),
            ByteArrayUtils.ToBigEndianBytesTrimmed(ephPubkey.Length));
        
        byte[] packetBase;

        if (record != null)
        {
            packetBase = ByteArrayUtils.Concatenate(authDataHead, idSignature, ephPubkey, record);
        }
        else
        {
            packetBase = ByteArrayUtils.Concatenate(authDataHead, idSignature, ephPubkey);
        }
        
        return packetBase;
    }
    
    public static HandshakePacket DecodeAuthData(byte[] authData)
    {
        var index = 0;
        var srcId = authData[..PacketConstants.NodeIdSize];
        index += PacketConstants.NodeIdSize;
        
        var sigSize = RlpExtensions.ByteArrayToInt32(authData[index..(index + PacketConstants.SigSize)]);
        index += PacketConstants.SigSize;
        
        var ephKeySize = RlpExtensions.ByteArrayToInt32(authData[index..(index + PacketConstants.EphemeralKeySize)]);
        index += PacketConstants.EphemeralKeySize;
        
        var idSignature = authData[index..(index + sigSize)];
        index += sigSize;
        
        var ephPubkey = authData[index..(index  + ephKeySize)];
        index += ephKeySize;
        
        var record = authData[index..];
        return new HandshakePacket(idSignature, ephPubkey, srcId, record);
    }
}