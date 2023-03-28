namespace Lantern.Discv5.Enr.Identity;

public interface IIdentitySchemeSigner
{
    byte[] SignRecord(EnrRecord record);
}