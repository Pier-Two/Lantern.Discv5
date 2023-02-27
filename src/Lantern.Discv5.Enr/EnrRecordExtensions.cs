using System.Net;
using System.Text;
using Lantern.Discv5.Enr.EntryType;
using Lantern.Discv5.Rlp;
using NeoSmart.Utils;

namespace Lantern.Discv5.Enr;

public static class EnrRecordExtensions
{
    public static byte[] EnrStringToBytes(string[] enrs)
    {
        using var stream = new MemoryStream();
        
        foreach(var enr in enrs)
        {
            var enrString = enr;
            
            if (enrString.StartsWith("enr:"))
            {
                enrString = enrString[4..];
            }
            
            stream.Write(UrlBase64.Decode(enrString));
        }
        
        return stream.ToArray();
    }
    
    public static EnrRecord FromBytes(byte[] bytes)
    {
        return CreateEnrRecord(bytes);
    }

    public static EnrRecord FromString(string enrString)
    {
        if (enrString.StartsWith("enr:"))
            enrString = enrString[4..];

        return CreateEnrRecord(UrlBase64.Decode(enrString));
    }

    private static EnrRecord CreateEnrRecord(byte[] bytes)
    {
        var items = RlpDecoder.Decode(bytes);
        var enrRecord = new EnrRecord
        {
            Signature = items[0],
            SequenceNumber = RlpExtensions.ByteArrayToUInt64(items[1])
        };

        for (var i = 2; i < items.Count - 1; i++)
        {
            var key = Encoding.ASCII.GetString(items[i]);
            var entry = GetEnrEntry(key, items[i + 1]);

            if (entry == null)
                continue;

            enrRecord.AddEntry(key, entry);
        }

        return enrRecord;
    }

    private static IEnrContentEntry? GetEnrEntry(string stringKey, byte[] value)
    {
        return stringKey switch
        {
            EnrContentKey.Id => new EntryId(Encoding.ASCII.GetString(value)),
            EnrContentKey.Ip => new EntryIp(new IPAddress(value)),
            EnrContentKey.Ip6 => new EntryIp6(new IPAddress(value)),
            EnrContentKey.Secp256K1 => new EntrySecp256K1(value),
            EnrContentKey.Tcp => new EntryTcp(RlpExtensions.ByteArrayToInt32(value)),
            EnrContentKey.Tcp6 => new EntryTcp6(RlpExtensions.ByteArrayToInt32(value)),
            EnrContentKey.Udp => new EntryUdp(RlpExtensions.ByteArrayToInt32(value)),
            EnrContentKey.Udp6 => new EntryUdp6(RlpExtensions.ByteArrayToInt32(value)),
            _ => default
        };
    }
}