using Lantern.Discv5.Enr;
using Lantern.Discv5.Enr.EnrContent;
using Lantern.Discv5.Enr.EnrContent.Entries;
using Lantern.Discv5.Enr.IdentityScheme.V4;

namespace Lantern.Discv5.WireProtocol.Table;

public class TableManager : ITableManager
{
    private const string DefaultIdentityScheme = "v4";
    
    public readonly KademliaTable.KBucket<NodeBucket> NodeRecordTable;
    
    public TableOptions Options { get; }
    
    public TableManager(TableOptions options)
    {
        NodeRecordTable = new KademliaTable.KBucket<NodeBucket>
        {
            ContactsPerBucket = options.BucketSize
        };
        Options = options;
        PopulateTable(options.BootstrapEnrs);
    }

    public void AddEnrRecord(NodeBucket nodeBucket)
    {
        NodeRecordTable.Add(nodeBucket);
    }

    public void RemoveEnrRecord(NodeBucket nodeBucket)
    {
        NodeRecordTable.Remove(nodeBucket);
    }

    public EnrRecord GetEnrRecord(byte[] nodeId)
    {
        NodeRecordTable.TryGet(nodeId, out var nodeBucket);
        return nodeBucket.Record;
    }
    
    private void PopulateTable(IEnumerable<EnrRecord> bootstrapEnrs)
    {
        foreach (var enrRecord in bootstrapEnrs)
        {
            var idType = enrRecord.GetEntry<EntryId>(EnrContentKey.Id).Value;

            if (idType == DefaultIdentityScheme)
            {
                var nodeBucket = new NodeBucket(enrRecord, new IdentitySchemeV4Verifier());
                NodeRecordTable.Add(nodeBucket);
            }
        }
    }
}