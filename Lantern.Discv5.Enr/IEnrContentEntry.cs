namespace Lantern.Discv5.Enr;

public interface IEnrContentEntry
{
    string Key { get; }

    byte[] EncodeEntry();
}