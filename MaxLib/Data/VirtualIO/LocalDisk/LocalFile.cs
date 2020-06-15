using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MaxLib.Data.VirtualIO.LocalDisk
{
    public class LocalFile : IVirtualFile
    {
        public FileInfo FileInfo { get; }

        public string Name => FileInfo.Name;

        public long? Length => FileInfo.Length;

        public DateTime? Created => FileInfo.CreationTimeUtc;

        public DateTime? Modified => FileInfo.LastWriteTimeUtc;

        public LocalHost Host { get; }

        public VirtualPath Path { get; }

        IIOHost IVirtualEntry.Host => Host;

        internal LocalFile(LocalHost host, FileInfo fileInfo, VirtualPath path)
            => (Host, FileInfo, Path) 
            = ( host ?? throw new ArgumentNullException(nameof(host)), 
                fileInfo ?? throw new ArgumentNullException(nameof(fileInfo)), 
                path ?? throw new ArgumentNullException(nameof(path)));

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

        public Stream GetStream(bool readOnly)
            => new FileStream(
                FileInfo.FullName,
                FileMode.OpenOrCreate,
                readOnly ? FileAccess.Read : FileAccess.ReadWrite,
                FileShare.Read);

        public Task<Stream> GetStreamAsync(bool readOnly)
            => GetStreamAsync(readOnly, CancellationToken.None);

        public Task<Stream> GetStreamAsync(bool readOnly, CancellationToken cancellationToken)
            => Task.Run(() => GetStream(readOnly), cancellationToken);
    }
}
