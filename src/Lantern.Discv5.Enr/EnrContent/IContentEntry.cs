namespace Lantern.Discv5.Enr.EnrContent;

public interface IContentEntry
{
    EnrContentKey Key { get; }

    IEnumerable<byte> EncodeEntry();
}