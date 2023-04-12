namespace Lantern.Discv5.Enr.EnrContent.Interfaces;

public interface IContentEntry
{
    EnrContentKey Key { get; }

    IEnumerable<byte> EncodeEntry();
}