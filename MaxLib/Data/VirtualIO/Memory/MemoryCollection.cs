using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MaxLib.Data.VirtualIO.Memory
{
    public class MemoryCollection : IVirtualCollection, IDisposable
    {
        public string Name { get; }

        public long? Length => null;

        public VirtualPath Path { get; }

        public DateTime? Created { get; } = DateTime.UtcNow;

        public DateTime? Modified { get; private set; } = DateTime.UtcNow;

        public MemoryHost Host { get; }

        IIOHost IVirtualEntry.Host => Host;

        private readonly Dictionary<string, IVirtualEntry> entries = new Dictionary<string, IVirtualEntry>();

        internal MemoryCollection(MemoryHost host, string name, VirtualPath path)
        {
            Host = host ?? throw new ArgumentNullException(nameof(host));
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Path = path ?? throw new ArgumentNullException(nameof(path));
        }

        public bool HasEntry(string name)
        {
            _ = name ?? throw new ArgumentNullException(nameof(name));
            return entries.ContainsKey(name);
        }

        public MemoryCollection AddCollection(string name)
        {
            _ = name ?? throw new ArgumentNullException(nameof(name));
            if (name.Contains("/"))
                throw new ArgumentException("name is not allowed to have slashes (/)", nameof(name));
            if (entries.ContainsKey(name))
                throw new ArgumentException("name is already used");
            var col = new MemoryCollection(Host, name, VirtualPath.Combine(Path, VirtualPath.Parse(name)));
            entries.Add(name, col);
            Modified = DateTime.UtcNow;
            return col;
        }

        public MemoryFile AddFile(string name, Stream stream)
        {
            _ = name ?? throw new ArgumentNullException(nameof(name));
            _ = stream ?? throw new ArgumentNullException(nameof(stream));
            return AddFile(name, () => Task.FromResult(stream));
        }

        public MemoryFile AddFile(string name, Task<Stream> stream)
        {
            _ = name ?? throw new ArgumentNullException(nameof(name));
            _ = stream ?? throw new ArgumentNullException(nameof(stream));
            return AddFile(name, () => stream);
        }

        public MemoryFile AddFile(string name, Func<Stream> getStream)
        {
            _ = name ?? throw new ArgumentNullException(nameof(name));
            _ = getStream ?? throw new ArgumentNullException(nameof(getStream));
            return AddFile(name, () => Task.FromResult(getStream()));
        }

        public MemoryFile AddFile(string name, Func<Task<Stream>> getStream)
        {
            _ = name ?? throw new ArgumentNullException(nameof(name));
            _ = getStream ?? throw new ArgumentNullException(nameof(getStream));
            if (name.Contains("/"))
                throw new ArgumentException("name is not allowed to have slashes (/)", nameof(name));
            if (entries.ContainsKey(name))
                throw new ArgumentException("name is already used");
            var file = new MemoryFile(Host, name, VirtualPath.Combine(Path, VirtualPath.Parse(name)), getStream);
            entries.Add(name, file);
            Modified = DateTime.UtcNow;
            return file;

        }

        public bool Remove(string name, bool dispose = true)
        {
            _ = name ?? throw new ArgumentNullException(nameof(name));
            if (dispose && entries.TryGetValue(name, out IVirtualEntry entry))
                if (entry is IDisposable disposable)
                    disposable.Dispose();
            return entries.Remove(name);
        }

        public void Dispose()
        {
            foreach (var pair in entries)
                if (pair.Value is IDisposable disposable)
                    disposable.Dispose();
            entries.Clear();
        }

        public IEnumerable<IVirtualCollection> GetCollections()
            => entries
                .Where(p => p.Value is IVirtualCollection)
                .Select(p => p.Value)
                .Cast<IVirtualCollection>();

        public Task<IEnumerable<IVirtualCollection>> GetCollectionsAsync()
            => Task.FromResult(GetCollections());

        public Task<IEnumerable<IVirtualCollection>> GetCollectionsAsync(CancellationToken cancellationToken)
            => Task.FromResult(GetCollections());

        public Task<DateTime?> GetCreatedAsync()
            => Task.FromResult(Created);

        public Task<DateTime?> GetCreatedAsync(CancellationToken cancellationToken)
            => Task.FromResult(Created);

        public IEnumerable<IVirtualEntry> GetEntries()
            => entries.Select(p => p.Value);

        public Task<IEnumerable<IVirtualEntry>> GetEntriesAsync()
            => Task.FromResult(GetEntries());

        public Task<IEnumerable<IVirtualEntry>> GetEntriesAsync(CancellationToken cancellationToken)
            => Task.FromResult(GetEntries());

        public IEnumerable<IVirtualFile> GetFiles()
            => entries
                .Where(p => p.Value is IVirtualFile)
                .Select(p => p.Value)
                .Cast<IVirtualFile>();

        public Task<IEnumerable<IVirtualFile>> GetFilesAsync()
            => Task.FromResult(GetFiles());

        public Task<IEnumerable<IVirtualFile>> GetFilesAsync(CancellationToken cancellationToken)
            => Task.FromResult(GetFiles());

        public Task<long?> GetLengthAsync()
            => Task.FromResult(Length);

        public Task<long?> GetLengthAsync(CancellationToken cancellationToken)
            => Task.FromResult(Length);

        public Task<DateTime?> GetModifiedAsync()
            => Task.FromResult(Modified);

        public Task<DateTime?> GetModifiedAsync(CancellationToken cancellationToken)
            => Task.FromResult(Modified);
    }
}
