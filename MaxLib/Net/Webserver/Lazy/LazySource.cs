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

        public override byte[] ReadSourcePart(long start, long length)
        {
            if (list == null) 
                list = GetAllSources().ToArray();
            using (var m = new MemoryStream((int)length))
            {
                foreach (var entry in list)
                {
                    var elength = entry.Length();
                    if (elength == null)
                    {
                        var block = entry.ReadSourcePart(0, start + length);
                        if (start < block.Length)
                        {
                            var readLength = Math.Min(length, block.Length - start);
                            m.Write(block, (int)start, (int)readLength);
                            length -= readLength;
                        }
                        start = Math.Max(0, start - block.Length);
                    }
                    else
                    {
                        if (start < elength.Value)
                        {
                            var readLength = Math.Min(length, elength.Value - start);
                            var block = entry.ReadSourcePart(start, readLength);
                            length -= readLength;
                            m.Write(block, 0, block.Length);
                        }
                        start = Math.Max(0, start - elength.Value);
                    }
                }
                return m.ToArray();
            }
        }

        public override long ReadFromStream(Stream networkStream, long readlength)
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
