namespace Lantern.Discv5.WireProtocol.Logging.Exceptions;

public class UdpTimeoutException : Exception
{
    public UdpTimeoutException(string message) : base(message) { }
}