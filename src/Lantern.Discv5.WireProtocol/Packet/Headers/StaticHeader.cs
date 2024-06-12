using System.Text;
using Lantern.Discv5.Rlp;

namespace Lantern.Discv5.WireProtocol.Packet.Headers;

public class StaticHeader
{
    public StaticHeader(string protocolId, byte[] version, byte[] authData, byte flag, byte[] nonce, int encryptedMessageLength = 0)
    {
        ProtocolId = protocolId;
        Version = version;
        AuthData = authData;
        AuthDataSize = AuthData.Length;
        Flag = flag;
        Nonce = nonce;
        EncryptedMessageLength = encryptedMessageLength;
    }

    public string ProtocolId { get; }

    public byte[] Version { get; }

    public byte[] AuthData { get; }

    public int AuthDataSize { get; }

    public byte Flag { get; }

    public byte[] Nonce { get; }

    public int EncryptedMessageLength { get; }

    public byte[] GetHeader()
    {
        var protocolId = Encoding.ASCII.GetBytes(ProtocolId);
        var authDataSize = ByteArrayUtils.ToBigEndianBytesTrimmed(AuthDataSize);
        var authDataBytes = new byte[PacketConstants.AuthDataSizeBytesLength];
        Array.Copy(authDataSize, 0, authDataBytes, PacketConstants.AuthDataSizeBytesLength - authDataSize.Length, authDataSize.Length);

        return ByteArrayUtils.Concatenate(protocolId, Version, new[] { Flag }, Nonce, authDataBytes, AuthData);
    }

    public static StaticHeader DecodeFromBytes(byte[] decryptedData)
    {
        var index = 0;
        var protocolId = Encoding.ASCII.GetString(decryptedData[..PacketConstants.ProtocolIdSize]);
        index += PacketConstants.ProtocolIdSize;

        var version = decryptedData[index..(index + PacketConstants.VersionSize)];
        index += PacketConstants.VersionSize;

        var flag = decryptedData[index];
        index += 1;

        var nonce = decryptedData[index..(index + PacketConstants.HeaderNonce)];
        index += PacketConstants.HeaderNonce;

        var authDataSize = RlpExtensions.ByteArrayToInt32(decryptedData[index..(index + PacketConstants.AuthDataSizeBytesLength)]);
        index += PacketConstants.AuthDataSizeBytesLength;

        // Based on the flag, it should retrieve the authdata correctly
        var authData = decryptedData[index..(index + authDataSize)];
        var encryptedMessage = decryptedData[(index + authDataSize)..];

        return new StaticHeader(protocolId, version, authData, flag, nonce, encryptedMessage.Length);
    }
}