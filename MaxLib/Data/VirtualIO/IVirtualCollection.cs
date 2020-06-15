using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MaxLib.Data.VirtualIO
{
    public interface IVirtualCollection : IVirtualEntry
    {
        IEnumerable<IVirtualEntry> GetEntries();

        Task<IEnumerable<IVirtualEntry>> GetEntriesAsync();

        Task<IEnumerable<IVirtualEntry>> GetEntriesAsync(CancellationToken cancellationToken);

        IEnumerable<IVirtualCollection> GetCollections();

        Task<IEnumerable<IVirtualCollection>> GetCollectionsAsync();

        Task<IEnumerable<IVirtualCollection>> GetCollectionsAsync(CancellationToken cancellationToken);

        IEnumerable<IVirtualFile> GetFiles();

        Task<IEnumerable<IVirtualFile>> GetFilesAsync();

        Task<IEnumerable<IVirtualFile>> GetFilesAsync(CancellationToken cancellationToken);

    }
}
