using MaxLib.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MaxLib.Net.Webserver.Lazy
{
    [Serializable]
    public class LazySource : HttpDataSource
    {
        public LazySource(WebProgressTask task, LazyEventHandler handler)
        {
            this.task = new LazyTask(task ?? throw new ArgumentNullException(nameof(task)));
            Handler = handler ?? throw new ArgumentNullException(nameof(handler));
        }

        public LazyEventHandler Handler { get; private set; }

        readonly LazyTask task;
        HttpDataSource[] list;

        public IEnumerable<HttpDataSource> GetAllSources()
        {
            return list ?? Handler(task);
        }

        public override long? Length()
        {
            if (list == null) 
                list = GetAllSources().ToArray();
            long sum = 0;
            foreach (var entry in list)
            {
                var length = entry.Length();
                if (length == null)
                    return null;
                sum += length.Value;
            }
            return sum;
        }

        public override void Dispose()
        {
            if (list != null)
                foreach (var s in list)
                    s.Dispose();
        }

        protected override async Task<long> WriteStreamInternal(Stream stream, long start, long? stop)
        {
            using (var skip = new SkipableStream(stream, start))
            {
                long total = 0;
                foreach (var s in GetAllSources())
                {
                    if (stop != null && total >= stop.Value)
                        return total;
                    var end = stop == null ? null : (long?)(stop.Value - total);
                    var size = s.Length();
                    if (size == null)
                    {
                        total += await s.WriteStream(skip, 0, end);
                    }
                    else
                    {
                        if (size.Value < skip.SkipBytes)
                        {
                            skip.Skip(size.Value);
                            continue;
                        }
                        var leftSkip = skip.SkipBytes;
                        skip.Skip(skip.SkipBytes);
                        total += await s.WriteStream(skip, leftSkip, end);
                    }
                }
                return total;
            }
        }

        protected override Task<long> ReadStreamInternal(Stream stream, long? length)
            => throw new NotSupportedException();

        public override long RangeStart
        {
            get => 0;
            set => throw new NotSupportedException();
        }

        public override long? RangeEnd
        {
            get => null;
            set => throw new NotSupportedException();
        }

        public override bool TransferCompleteData
        {
            get => true;
            set => throw new NotSupportedException();
        }

        public override bool CanAcceptData => false;

        public override bool CanProvideData => true;
    }
}
