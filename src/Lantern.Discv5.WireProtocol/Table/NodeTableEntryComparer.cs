using Lantern.Discv5.Rlp;

namespace Lantern.Discv5.WireProtocol.Table;

public class NodeTableEntryComparer : EqualityComparer<NodeTableEntry>
{
    public override bool Equals(NodeTableEntry x, NodeTableEntry y)
    {
        if (x == null || y == null)
        {
            return false;
        }

        return x.Id.SequenceEqual(y.Id);
    }

    public override int GetHashCode(NodeTableEntry obj)
    {
        if (obj == null)
        {
            return 0;
        }

        return BitConverter.ToInt32(obj.Id, 0);
    }
}