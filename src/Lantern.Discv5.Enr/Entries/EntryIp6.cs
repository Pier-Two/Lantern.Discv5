using System.Net;
using System.Text;
using Lantern.Discv5.Rlp;

namespace Lantern.Discv5.Enr.Entries;

public class EntryIp6 : IEntry
{
    public EntryIp6(IPAddress value)
    {
        Value = value;
    }

    public IPAddress Value { get; }

    public EnrEntryKey Key => EnrEntryKey.Ip6;

    public IEnumerable<byte> EncodeEntry()
    {
        return ByteArrayUtils.JoinByteArrays(RlpEncoder.EncodeString(Key, Encoding.ASCII),
            RlpEncoder.EncodeBytes(Value.GetAddressBytes()));
    }
}