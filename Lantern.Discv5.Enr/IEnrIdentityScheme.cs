namespace Lantern.Discv5.Enr;

public interface IEnrIdentityScheme
{
    byte[] SignEnrRecord(EnrRecord enrRecord);
}