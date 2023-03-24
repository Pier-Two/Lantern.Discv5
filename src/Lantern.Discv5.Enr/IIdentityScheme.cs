namespace Lantern.Discv5.Enr;

public interface IIdentityScheme
{
    byte[] SignEnrRecord(EnrRecord record);
}