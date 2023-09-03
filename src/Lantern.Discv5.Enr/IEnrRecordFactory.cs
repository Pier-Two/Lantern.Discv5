using Lantern.Discv5.Enr.IdentityScheme.Interfaces;

namespace Lantern.Discv5.Enr;

public interface IEnrRecordFactory
{
    EnrRecord CreateFromString(string enrString, IIdentitySchemeVerifier verifier);

    EnrRecord CreateFromBytes(byte[] bytes, IIdentitySchemeVerifier verifier);

    EnrRecord[] CreateFromMultipleEnrList(IEnumerable<IEnumerable<byte[]>> enrs, IIdentitySchemeVerifier verifier);

    EnrRecord CreateFromDecoded(IReadOnlyList<byte[]> items, IIdentitySchemeVerifier verifier);
}