namespace Lantern.Discv5.WireProtocol.Logging.Exceptions;

public class InvalidPacketException : Exception
{
    public InvalidPacketException(string message) : base(message) { }
}