using Lantern.Discv5.Enr;

namespace Lantern.Discv5.WireProtocol.Table;

public interface ITableManager
{
    public TableOptions Options { get; }
    
    public void AddEnrRecord(NodeBucket nodeBucket);
    
    public EnrRecord GetEnrRecord(byte[] nodeId);
}