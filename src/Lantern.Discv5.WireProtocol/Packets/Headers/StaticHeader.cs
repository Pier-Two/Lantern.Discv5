using System.Text;
using Lantern.Discv5.Rlp;
using Lantern.Discv5.WireProtocol.Packets.Constants;

namespace Lantern.Discv5.WireProtocol.Packets.Headers;

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

    public readonly string ProtocolId;

    public readonly byte[] Version;

    public readonly byte[] AuthData;

    public readonly int AuthDataSize;

    public readonly byte Flag;

    public readonly byte[] Nonce;

    public readonly int EncryptedMessageLength;

    public byte[] GetHeader()
    {
        var protocolId = Encoding.ASCII.GetBytes(ProtocolId);
        var authDataSize = ByteArrayUtils.ToBigEndianBytesTrimmed(AuthDataSize);
        var authDataBytes = new byte[AuthDataSizes.AuthDataSizeBytesLength];
        Array.Copy(authDataSize, 0, authDataBytes, AuthDataSizes.AuthDataSizeBytesLength - authDataSize.Length, authDataSize.Length);

        return ByteArrayUtils.Concatenate(protocolId, Version, new[] { Flag }, Nonce, authDataBytes, AuthData);
    }

    public static StaticHeader DecodeFromBytes(byte[] decryptedData)
    {
        var index = 0;
        var protocolId = Encoding.ASCII.GetString(decryptedData[..AuthDataSizes.ProtocolIdSize]);
        index += AuthDataSizes.ProtocolIdSize;
        
        var version = decryptedData[index..(index + AuthDataSizes.VersionSize)];
        index += AuthDataSizes.VersionSize;
        
        var flag = decryptedData[index];
        index += 1;
        
        var nonce = decryptedData[index..(index + AuthDataSizes.HeaderNonce)];
        index += AuthDataSizes.HeaderNonce;
        
        var authDataSize = RlpExtensions.ByteArrayToInt32(decryptedData[index..(index + AuthDataSizes.AuthDataSizeBytesLength)]);
        index += AuthDataSizes.AuthDataSizeBytesLength;
        
        // Based on the flag, it should retrieve the authdata correctly
        var authData = decryptedData[index..(index + authDataSize)];
        
        var encryptedMessage = decryptedData[(index + authDataSize)..];
        return new StaticHeader(protocolId, version, authData, flag, nonce, encryptedMessage.Length);
    }
}