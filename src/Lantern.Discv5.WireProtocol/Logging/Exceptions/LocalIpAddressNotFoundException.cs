namespace Lantern.Discv5.WireProtocol.Logging.Exceptions;

public class LocalIpAddressNotFoundException : Exception
{
    public LocalIpAddressNotFoundException(string message) : base(message)
    {
    }
}