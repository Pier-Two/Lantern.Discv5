using System.Runtime.Serialization;

namespace Lantern.Discv5.WireProtocol.Logging.Exceptions;

[Serializable]
public class InvalidPacketException : Exception
{
    public InvalidPacketException(string message) : base(message) { }

    protected InvalidPacketException(SerializationInfo info, StreamingContext context) : base(info, context) { }

    public override void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        if (info == null)
        {
            throw new ArgumentNullException(nameof(info));
        }

        base.GetObjectData(info, context);
    }
}