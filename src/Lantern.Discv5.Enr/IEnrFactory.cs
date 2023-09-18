using Lantern.Discv5.Enr.Identity;

namespace Lantern.Discv5.Enr;

public interface IEnrFactory
{
    Enr CreateFromString(string enrString, IIdentityVerifier verifier);

    Enr CreateFromBytes(byte[] bytes, IIdentityVerifier verifier);

    Enr[] CreateFromMultipleEnrList(IEnumerable<IEnumerable<byte[]>> enrs, IIdentityVerifier verifier);

    Enr CreateFromDecoded(IReadOnlyList<byte[]> items, IIdentityVerifier verifier);
}