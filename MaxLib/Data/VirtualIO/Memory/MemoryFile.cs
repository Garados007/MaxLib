using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MaxLib.Data.VirtualIO.Memory
{
    public class MemoryFile : IVirtualFile, IDisposable
    {
        private readonly Func<Task<Stream>> createTask;
        private Task<Stream> bufferedTask;

        public string Name { get; }

        public long? Length
        {
            get
            {
                if (bufferedTask == null || !bufferedTask.IsCompleted)
                    return null;
                if (bufferedTask.Result.CanSeek)
                    return bufferedTask.Result.Length;
                else return null;
            }
        }

        public VirtualPath Path { get; }

        public DateTime? Created => DateTime.UtcNow;

        public DateTime? Modified => null;

        public MemoryHost Host { get; }

        IIOHost IVirtualEntry.Host => Host;

        internal MemoryFile(MemoryHost host, string name, VirtualPath path, Func<Task<Stream>> createTask)
        {
            Host = host ?? throw new ArgumentNullException(nameof(host));
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Path = path ?? throw new ArgumentNullException(nameof(path));
            this.createTask = createTask ?? throw new ArgumentNullException(nameof(createTask));
        }

        public Task<DateTime?> GetCreatedAsync()
            => Task.FromResult(Created);

        public Task<DateTime?> GetCreatedAsync(CancellationToken cancellationToken)
            => Task.FromResult(Created);

        public Task<long?> GetLengthAsync()
            => GetLengthAsync(CancellationToken.None);

        public async Task<long?> GetLengthAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (bufferedTask == null)
                bufferedTask = createTask();
            await Task.WhenAny(bufferedTask, Task.FromCanceled(cancellationToken));
            cancellationToken.ThrowIfCancellationRequested();
            var stream = await bufferedTask;
            if (stream.CanSeek)
                return stream.Length;
            else return null;
        }

        public Task<DateTime?> GetModifiedAsync()
            => Task.FromResult(Modified);

        public Task<DateTime?> GetModifiedAsync(CancellationToken cancellationToken)
            => Task.FromResult(Modified);

        public Stream GetStream(bool readOnly)
        {
            if (bufferedTask == null)
                bufferedTask = createTask();
            return bufferedTask.Result;
        }

        public Task<Stream> GetStreamAsync(bool readOnly)
            => GetStreamAsync(readOnly, CancellationToken.None);

        public async Task<Stream> GetStreamAsync(bool readOnly, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (bufferedTask == null)
                bufferedTask = createTask();
            await Task.WhenAny(bufferedTask, Task.FromCanceled(cancellationToken));
            cancellationToken.ThrowIfCancellationRequested();
            return await bufferedTask;
        }

        public void Dispose()
        {
            if (bufferedTask != null)
            {
                bufferedTask.ContinueWith(async (t) =>
                {
                    (await t).Dispose();
                });
                if (bufferedTask.IsCanceled)
                    bufferedTask.Result.Dispose();
                bufferedTask = null;
            }
        }
    }
}
