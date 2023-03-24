namespace Lantern.Discv5.Enr;

public interface IContentEntry
{
    string Key { get; }

    byte[] EncodeEntry();
}