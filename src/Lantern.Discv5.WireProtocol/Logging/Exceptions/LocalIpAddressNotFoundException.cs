using System.Runtime.Serialization;

namespace Lantern.Discv5.WireProtocol.Logging.Exceptions;

[Serializable]
public class LocalIpAddressNotFoundException : Exception
{
    public LocalIpAddressNotFoundException(string message) : base(message) { }
    
    protected LocalIpAddressNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context) { }

    public override void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        if (info == null)
        {
            throw new ArgumentNullException(nameof(info));
        }

        base.GetObjectData(info, context);
    }
}