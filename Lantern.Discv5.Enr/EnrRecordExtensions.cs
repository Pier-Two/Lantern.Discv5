using System.Net;
using System.Text;
using Lantern.Discv5.Enr.EntryType;
using Lantern.Discv5.Rlp;
using NeoSmart.Utils;

namespace Lantern.Discv5.Enr;

public static class EnrRecordExtensions
{
    public static EnrRecord CreateEnrRecord(string enrString)
    {
        if (enrString.StartsWith("enr:")) enrString = enrString[4..];
        return CreateFromBytes(UrlBase64.Decode(enrString));
    }

    private static EnrRecord CreateFromBytes(byte[] bytes)
    {
        var items = RlpDecoder.Decode(bytes);
        var enrRecord = new EnrRecord
        {
            Signature = items[0],
            SequenceNumber = Utility.ByteArrayToUInt64(items[1])
        };

        for (var i = 2; i < items.Count - 1; i++)
        {
            var stringKey = GetEnrContentKey(items[i]);

            if (stringKey == null) continue;

            var value = items[i + 1];
            enrRecord.AddEntry(GenerateEnrContent(stringKey, value));
        }

        return enrRecord;
    }

    private static string? GetEnrContentKey(byte[] data)
    {
        var stringKey = Encoding.ASCII.GetString(data);

        return stringKey switch
        {
            EnrContentKey.Id => EnrContentKey.Id,
            EnrContentKey.Ip => EnrContentKey.Ip,
            EnrContentKey.Ip6 => EnrContentKey.Ip6,
            EnrContentKey.Secp256K1 => EnrContentKey.Secp256K1,
            EnrContentKey.Tcp => EnrContentKey.Tcp,
            EnrContentKey.Tcp6 => EnrContentKey.Tcp6,
            EnrContentKey.Udp => EnrContentKey.Udp,
            EnrContentKey.Udp6 => EnrContentKey.Udp6,
            _ => null
        };
    }

    private static EnrContentEntry GenerateEnrContent(string key, byte[] value)
    {
        return key switch
        {
            EnrContentKey.Id => new EntryId(Encoding.ASCII.GetString(value)),
            EnrContentKey.Ip => new EntryIp(new IPAddress(value)),
            EnrContentKey.Ip6 => new EntryIp6(new IPAddress(value)),
            EnrContentKey.Secp256K1 => new EntrySecp256K1(value),
            EnrContentKey.Tcp => new EntryTcp((int)Utility.ByteArrayToUInt64(value)),
            EnrContentKey.Tcp6 => new EntryTcp6((int)Utility.ByteArrayToUInt64(value)),
            EnrContentKey.Udp => new EntryUdp((int)Utility.ByteArrayToUInt64(value)),
            EnrContentKey.Udp6 => new EntryUdp6((int)Utility.ByteArrayToUInt64(value)),
            _ => throw new ArgumentOutOfRangeException(nameof(key), key, null)
        };
    }
}