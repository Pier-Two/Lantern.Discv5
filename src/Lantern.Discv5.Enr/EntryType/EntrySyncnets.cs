using System.Text;
using Lantern.Discv5.Rlp;

namespace Lantern.Discv5.Enr.EntryType;

public class EntrySyncnets : IEnrContentEntry
{
    public EntrySyncnets(string value)
    {
        Value = value;
    }
    
    public string Value { get; }
    
    public string Key => EnrContentKey.Syncnets;
    
    public byte[] EncodeEntry()
    {
        return Helpers.JoinByteArrays(RlpEncoder.EncodeString(Key, Encoding.ASCII),
                    RlpEncoder.EncodeHexString(Value));
    }
}