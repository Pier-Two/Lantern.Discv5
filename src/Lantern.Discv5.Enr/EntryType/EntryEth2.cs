using System.Text;
using Lantern.Discv5.Rlp;

namespace Lantern.Discv5.Enr.EntryType;

public class EntryEth2 : IContentEntry
{
    public EntryEth2(byte[] value)
    {
        Value = value;
    }

    public byte[] Value { get; }

    public string Key => EnrContentKey.Eth2;

    public byte[] EncodeEntry()
    {
        return ByteArrayUtils.JoinByteArrays(RlpEncoder.EncodeString(Key, Encoding.ASCII),
            RlpEncoder.EncodeBytes(Value));
    }
}