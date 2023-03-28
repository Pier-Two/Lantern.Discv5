namespace Lantern.Discv5.Enr.Content;

public interface IContentEntry
{
    string Key { get; }

    IEnumerable<byte> EncodeEntry();
}