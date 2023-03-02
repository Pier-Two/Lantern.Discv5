using System.Text;
using Lantern.Discv5.Rlp;

namespace Lantern.Discv5.Enr.EntryType;

public class EntryAttnets : IEnrContentEntry
{
    public EntryAttnets(byte[] value)
    {
        Value = value;
    }

    public string Key => EnrContentKey.Attnets;
    
    public byte[] Value { get; }
    
    public byte[] EncodeEntry()
    {
        return Helpers.JoinByteArrays(RlpEncoder.EncodeString(Key, Encoding.ASCII),
            RlpEncoder.EncodeBytes(Value));
    }
}