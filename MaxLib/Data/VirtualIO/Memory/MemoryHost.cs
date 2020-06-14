using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MaxLib.Data.VirtualIO.Memory
{
    public class MemoryHost : IIOHost, IDisposable
    {
        public VirtualPath BasePath { get; }

        public string IODesc => $"Memory IO: {BasePath}";

        public RootController Root { get; }

        public MemoryCollection RootCollection { get; }

        public MemoryHost(RootController rootController, VirtualPath basePath = null)
        {
            Root = rootController ?? throw new ArgumentNullException(nameof(rootController));
            BasePath = basePath ?? VirtualPath.Parse("");
            RootCollection = new MemoryCollection(this, "", BasePath);
        }

        public IEnumerable<IVirtualEntry> GetEntries(VirtualPath path)
        {
            _ = path ?? throw new ArgumentNullException(nameof(path));
            path = path.MakeRootPath();
            var comp = BasePath.CompareTo(path, true, true);
            if (comp == null || comp.Value > 0)
                return Enumerable.Empty<IVirtualEntry>();

            path = path.RemoveLocks();
            var relative = path.CreateSubPath(BasePath.Length, false);
            IEnumerable<IVirtualEntry> result = new IVirtualEntry[] { RootCollection };
            foreach (var part in relative)
            {
                if (part == "@")
                    continue;
                result = Filter(result, part);
            }
            return result;
        }

        private IEnumerable<IVirtualEntry> Filter(IEnumerable<IVirtualEntry> entries, string name)
        {
            return entries.SelectMany(entry =>
            {
                if (entry is IVirtualCollection collection)
                {
                    return collection.GetEntries()
                        .Where(e => e.Name == name);
                }
                else return Enumerable.Empty<IVirtualEntry>();
            });
        }

        public Task<IEnumerable<IVirtualEntry>> GetEntriesAsync(VirtualPath path)
            => GetEntriesAsync(path, CancellationToken.None);

        public Task<IEnumerable<IVirtualEntry>> GetEntriesAsync(VirtualPath path, CancellationToken cancellationToken)
        {
            _ = path ?? throw new ArgumentNullException(nameof(path));
            cancellationToken.ThrowIfCancellationRequested();
            return Task.Run(() => GetEntries(path), cancellationToken);
        }

        public void Dispose()
            => RootCollection.Dispose();
    }
}
