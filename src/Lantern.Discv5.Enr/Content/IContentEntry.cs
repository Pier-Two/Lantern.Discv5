namespace Lantern.Discv5.Enr.Content;

public interface IContentEntry
{
    EnrContentKey Key { get; }

    IEnumerable<byte> EncodeEntry();
}