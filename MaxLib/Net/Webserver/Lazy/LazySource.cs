using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MaxLib.Net.Webserver.Lazy
{
    [Serializable]
    public class LazySource : HttpDataSource
    {
        public LazySource(WebProgressTask task, LazyEventHandler handler)
        {
            this.task = new LazyTask(task ?? throw new ArgumentNullException("task"));
            Handler = handler ?? throw new ArgumentNullException("handler");
        }

        public LazyEventHandler Handler { get; private set; }

        readonly LazyTask task;
        HttpDataSource[] list;

        public IEnumerable<HttpDataSource> GetAllSources()
        {
            return list ?? Handler(task);
        }

        public override long AproximateLength()
        {
            if (list == null) list = GetAllSources().ToArray();
            return list.Sum((s) => s.AproximateLength());
        }

        public override void Dispose()
        {
            if (list != null)
                foreach (var s in list)
                    s.Dispose();
        }

        public override byte[] GetSourcePart(long start, long length)
        {
            if (list == null) list = GetAllSources().ToArray();
            using (var m = new MemoryStream((int)length))
            {
                for (int i = 0; i < list.Length; ++i)
                {
                    var apl = list[i].AproximateLength();
                    if (start < apl)
                    {
                        var rl = Math.Min(length, apl - start);
                        var b = list[i].GetSourcePart(start, rl);
                        length -= rl;
                        m.Write(b, 0, b.Length);
                    }
                    start -= apl;
                }
                return m.ToArray();
            }
        }

        public override long ReadFromStream(Stream networkStream, long readlength)
        {
            return 0;
        }

        public override long ReserveExtraMemory(long bytes)
        {
            return 0;
        }

        public override int WriteSourcePart(byte[] source, long start, long length)
        {
            return 0;
        }

        public override long WriteToStream(Stream networkStream)
        {
            long length = 0;
            foreach (var s in GetAllSources())
                length += s.WriteToStream(networkStream);
            return length;
        }

#pragma warning disable CS0809
        [Obsolete("This is not supported in this class.", true)]
        public override long RangeStart
        {
            get => base.RangeStart;
            set => base.RangeStart = value;
        }

        [Obsolete("This is not supported in this class.", true)]
        public override long RangeEnd
        {
            get => base.RangeEnd;
            set => base.RangeEnd = value;
        }

        [Obsolete("This is not supported in this class.", true)]
        public override bool TransferCompleteData
        {
            get => base.TransferCompleteData;
            set => base.TransferCompleteData = value;
        }
#pragma warning restore CS0809
    }
}
