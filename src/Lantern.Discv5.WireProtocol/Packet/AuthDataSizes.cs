namespace Lantern.Discv5.WireProtocol.Packet;

public struct AuthDataSizes
{
    public const int Ordinary = 32;
    public const int WhoAreYou = 24;
    public const int Handshake = 34;
    public const int HeaderNonce = 12;
    public const int IdNonceSize = 16;
    public const int EnrSeqSize = 8;
    public const int SigSize = 1;
    public const int EphemeralKeySize = 1;
    public const int NodeIdSize = 32;
    public const int AuthDataSizeBytesLength = 2;
    public const int ProtocolIdSize = 6;
    public const int VersionSize = 2;
}