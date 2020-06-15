using MaxLib.Collections;
using MaxLib.Data.VirtualIO.LocalDisk;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MaxLib.Data.VirtualIO
{
    public class RootController : IDisposable
    {
        public HashSet<IIOHost> IOHosts { get; } = new HashSet<IIOHost>();

        public virtual IEnumerable<IVirtualEntry> Get(VirtualPath path)
        {
            _ = path ?? throw new ArgumentNullException(nameof(path));
            var builder = new EnumeratorBuilder<IVirtualEntry>();
            if (path.HasHostFilter)
            {
                var filter = path.GetHostFilter();
                foreach (var host in IOHosts)
                    if (host.BasePath.Equals(filter, true, true))
                        builder.Yield(() => host.GetEntries(path));
            }
            else
            {
                foreach (var host in IOHosts)
                    builder.Yield(() => host.GetEntries(path));
            }
            return builder;
        }

        public virtual Task<IEnumerable<IVirtualEntry>> GetAsync(VirtualPath path)
            => GetAsync(path, CancellationToken.None);

        public virtual async Task<IEnumerable<IVirtualEntry>> GetAsync(VirtualPath path, CancellationToken cancellationToken)
        {
            _ = path ?? throw new ArgumentNullException(nameof(path));
            cancellationToken.ThrowIfCancellationRequested();
            IEnumerable<IIOHost> hosts = IOHosts;
            if (path.HasHostFilter)
            {
                var filter = path.GetHostFilter();
                hosts = hosts.Where(host => host.BasePath.Equals(filter, true, true));
            }
            var list = hosts.Select(host => host.GetEntriesAsync(path, cancellationToken));
            return (await Task.WhenAll(list)).SelectMany(e => e);
        }

        public virtual IEnumerable<T> Get<T>(VirtualPath path)
            where T : IVirtualEntry
            => Get(path).Where(entry => entry is T).Cast<T>();

        public virtual Task<IEnumerable<T>> GetAsync<T>(VirtualPath path)
            where T : IVirtualEntry
            => GetAsync<T>(path, CancellationToken.None);

        public virtual async Task<IEnumerable<T>> GetAsync<T>(VirtualPath path, CancellationToken cancellationToken)
            where T : IVirtualEntry
            => (await GetAsync(path, cancellationToken))
                .Where(entry => entry is T)
                .Cast<T>();

        public virtual LocalHost AddLocalHost(DirectoryInfo rootDirectory, VirtualPath basePath = null)
        {
            _ = rootDirectory ?? throw new ArgumentNullException(nameof(rootDirectory));
            var host = new LocalHost(this, rootDirectory, basePath);
            IOHosts.Add(host);
            return host;
        }

        public virtual Memory.MemoryHost AddMemoryHost(VirtualPath basePath = null)
        {
            var host = new Memory.MemoryHost(this, basePath);
            IOHosts.Add(host);
            return host;
        }

        public void Dispose()
        {
            foreach (var host in IOHosts)
                if (host is IDisposable disposable)
                    disposable.Dispose();
        }
    }
}
