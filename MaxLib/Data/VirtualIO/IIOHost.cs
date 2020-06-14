using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MaxLib.Data.VirtualIO
{
    public interface IIOHost
    {
        VirtualPath BasePath { get; }

        IEnumerable<IVirtualEntry> GetEntries(VirtualPath path);

        Task<IEnumerable<IVirtualEntry>> GetEntriesAsync(VirtualPath path);

        Task<IEnumerable<IVirtualEntry>> GetEntriesAsync(VirtualPath path, CancellationToken cancellationToken);

        /// <summary>
        /// Some arbitary description about the io source. This is usefull if a user 
        /// want to get some hint about this.
        /// </summary>
        string IODesc { get; }

        RootController Root { get; }
    }
}
