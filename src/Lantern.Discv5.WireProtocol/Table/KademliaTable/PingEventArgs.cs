namespace Lantern.Discv5.WireProtocol.Table.KademliaTable
{
    /// <summary>
    ///   The contacts that should be checked.
    /// </summary>
    /// <seealso cref="KBucket{T}.Ping"/>
    public class PingEventArgs<T> : EventArgs
         where T : IContact
    {
        /// <summary>
        ///   The contacts that should be checked.
        /// </summary>
        public IEnumerable<T> Oldest;

        /// <summary>
        ///   A new contact that wants to be added.
        /// </summary>
        public T Newest;
    }
}
