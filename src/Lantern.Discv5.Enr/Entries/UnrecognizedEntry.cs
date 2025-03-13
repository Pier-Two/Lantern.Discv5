using System.Text;
using Lantern.Discv5.Rlp;
using static Lantern.Discv5.Rlp.RlpDecoder;

namespace Lantern.Discv5.Enr.Entries;

public class UnrecognizedEntry(string key, RlpStruct valueRlp) : IEntry
{
    public string Key { get; } = key;
    public byte[] Value { get; } = valueRlp.GetData();

    EnrEntryKey IEntry.Key => new(Key);

    public IEnumerable<byte> EncodeEntry()
    {
        return ByteArrayUtils.JoinByteArrays(RlpEncoder.EncodeString(Key, Encoding.ASCII), valueRlp.GetRlp());
    }
}