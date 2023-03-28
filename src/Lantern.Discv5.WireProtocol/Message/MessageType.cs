namespace Lantern.Discv5.WireProtocol.Message;

public static class MessageType
{
    public const byte Ping = 0x01;

    public const byte Pong = 0x02;

    public const byte FindNode = 0x03;

    public const byte Nodes = 0x04;

    public const byte TalkReq = 0x05;

    public const byte TalkResp = 0x06;

    public const byte RegTopic = 0x07;

    public const byte Ticket = 0x08;

    public const byte RegConfirmation = 0x09;

    public const byte TopicQuery = 0x0A;
}