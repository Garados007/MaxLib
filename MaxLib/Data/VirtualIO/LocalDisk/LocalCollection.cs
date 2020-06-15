using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MaxLib.Data.VirtualIO.LocalDisk
{
    public class LocalCollection : IVirtualCollection
    {
        public DirectoryInfo DirectoryInfo { get; }

        public string Name => DirectoryInfo.Name;

        public long? Length => null;

        public VirtualPath Path { get; }

        public DateTime? Created => DirectoryInfo.CreationTimeUtc;

        public DateTime? Modified => DirectoryInfo.LastWriteTimeUtc;

        public LocalHost Host { get; }

        IIOHost IVirtualEntry.Host => Host;

        internal LocalCollection(LocalHost host, DirectoryInfo directoryInfo, VirtualPath path)
            => (Host, DirectoryInfo, Path)
            = (host ?? throw new ArgumentNullException(nameof(host)),
                directoryInfo ?? throw new ArgumentNullException(nameof(directoryInfo)),
                path ?? throw new ArgumentNullException(nameof(path)));

        public IEnumerable<IVirtualCollection> GetCollections()
            => DirectoryInfo.EnumerateDirectories()
                .Select(info => new LocalCollection(
                    Host, 
                    info, 
                    VirtualPath.Combine(Path, VirtualPath.Parse(info.Name))))
                .Cast<IVirtualCollection>();

        public Task<IEnumerable<IVirtualCollection>> GetCollectionsAsync()
            => GetCollectionsAsync(CancellationToken.None);

        public Task<IEnumerable<IVirtualCollection>> GetCollectionsAsync(CancellationToken cancellationToken)
            => Task.Run(() => GetCollections(), cancellationToken);

        public Task<DateTime?> GetCreatedAsync()
            => Task.FromResult(Created);

        public Task<DateTime?> GetCreatedAsync(CancellationToken cancellationToken)
            => Task.FromResult(Created);

        public Task<long?> GetLengthAsync()
            => Task.FromResult(Length);

        public Task<long?> GetLengthAsync(CancellationToken cancellationToken)
            => Task.FromResult(Length);

        public Task<DateTime?> GetModifiedAsync()
            => Task.FromResult(Modified);

        public Task<DateTime?> GetModifiedAsync(CancellationToken cancellationToken)
            => Task.FromResult(Modified);

        public IEnumerable<IVirtualEntry> GetEntries()
            => GetCollections()
                .Cast<IVirtualEntry>()
                .Concat(
                    GetFiles().Cast<IVirtualEntry>()
                );

        public Task<IEnumerable<IVirtualEntry>> GetEntriesAsync()
            => GetEntriesAsync(CancellationToken.None);

        public Task<IEnumerable<IVirtualEntry>> GetEntriesAsync(CancellationToken cancellationToken)
            => Task.Run(() => GetEntries(), cancellationToken);

        public IEnumerable<IVirtualFile> GetFiles()
            => DirectoryInfo.EnumerateFiles()
                .Select(info => new LocalFile(
                    Host,
                    info,
                    VirtualPath.Combine(Path, VirtualPath.Parse(info.Name))))
                .Cast<IVirtualFile>();

        public Task<IEnumerable<IVirtualFile>> GetFilesAsync()
            => GetFilesAsync(CancellationToken.None);

        public Task<IEnumerable<IVirtualFile>> GetFilesAsync(CancellationToken cancellationToken)
            => Task.Run(() => GetFiles(), cancellationToken);
    }
}
