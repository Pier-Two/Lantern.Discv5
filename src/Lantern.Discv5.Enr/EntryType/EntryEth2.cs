using System.Text;
using Lantern.Discv5.Rlp;

namespace Lantern.Discv5.Enr.EntryType;

public class EntryEth2 : IEnrContentEntry
{
    public EntryEth2(byte[] value)
    {
        Value = value;
    }

    public string Key => EnrContentKey.Eth2;
    
    public byte[] Value { get; }
    
    public byte[] EncodeEntry()
    {
        return Helpers.JoinByteArrays(RlpEncoder.EncodeString(Key, Encoding.ASCII),
            RlpEncoder.EncodeBytes(Value));
    }
}