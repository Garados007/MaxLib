using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MaxLib.Data.VirtualIO.LocalDisk
{
    public class LocalHost : IIOHost, IEquatable<LocalHost>
    {
        public VirtualPath BasePath { get; }

        public string IODesc => $"Local IO: {RootDirectory} -> {BasePath}";

        public RootController Root { get; }

        public DirectoryInfo RootDirectory { get; }

        public LocalHost(RootController rootController, DirectoryInfo rootDirectory, VirtualPath basePath = null)
        {
            Root = rootController ?? throw new ArgumentNullException(nameof(rootController));
            RootDirectory = rootDirectory ?? throw new ArgumentNullException(nameof(rootDirectory));
            if (!rootDirectory.Exists)
                throw new DirectoryNotFoundException("root directory not found");
            BasePath = basePath?.MakeRootPath()?.RemoveLocks() ?? VirtualPath.Parse("");
        }

        public IEnumerable<IVirtualEntry> GetEntries(VirtualPath path)
        {
            _ = path ?? throw new ArgumentNullException(nameof(path));
            path = path.MakeRootPath();
            var comp = BasePath.CompareTo(path, true, true);
            if (comp == null || comp.Value > 0)
                yield break;

            path = path.RemoveLocks();
            var relative = path.CreateSubPath(BasePath.Length, false);
            var pathTiles = new[] { RootDirectory.FullName }
                .Concat(relative.Where(t => t != "@"));
            var localPath = Path.Combine(pathTiles.ToArray());

            if (Directory.Exists(localPath))
                yield return new LocalCollection(this, new DirectoryInfo(localPath), path);
            if (File.Exists(localPath))
                yield return new LocalFile(this, new FileInfo(localPath), path);
        }

        public Task<IEnumerable<IVirtualEntry>> GetEntriesAsync(VirtualPath path)
            => GetEntriesAsync(path, CancellationToken.None);

        public Task<IEnumerable<IVirtualEntry>> GetEntriesAsync(VirtualPath path, CancellationToken cancellationToken)
        {
            _ = path ?? throw new ArgumentNullException(nameof(path));
            return Task.Run(() => GetEntries(path), cancellationToken);
        }

        public virtual bool Equals(LocalHost other)
        {
            if (other is null)
                return false;
            return RootDirectory.FullName == other.RootDirectory.FullName
                && BasePath == other.BasePath;
        }

        public override bool Equals(object obj)
        {
            if (obj is LocalHost host)
                return Equals(host);
            else return false;
        }

        public override int GetHashCode()
        {
            return RootDirectory.FullName.GetHashCode() ^ BasePath.GetHashCode();
        }

        public override string ToString()
            => IODesc;

        public static bool operator ==(LocalHost host1, LocalHost host2)
            => Equals(host1, host2);

        public static bool operator !=(LocalHost host1, LocalHost host2)
            => !Equals(host1, host2);
    }
}
