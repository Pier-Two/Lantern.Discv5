using Lantern.Discv5.Enr.Identity;
using static Lantern.Discv5.Rlp.RlpDecoder;

namespace Lantern.Discv5.Enr;

public interface IEnrFactory
{
    Enr CreateFromString(string enrString, IIdentityVerifier verifier);

    Enr CreateFromBytes(byte[] bytes, IIdentityVerifier verifier);

    Enr[] CreateFromMultipleEnrList(ReadOnlySpan<RlpStruct> enrs, IIdentityVerifier verifier);

    Enr CreateFromRlp(RlpStruct enrRlp, IIdentityVerifier verifier);
}