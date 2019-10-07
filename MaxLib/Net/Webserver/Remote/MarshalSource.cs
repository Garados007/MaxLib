using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaxLib.Net.Webserver.Remote
{
    [Serializable]
    public class MarshalSource : HttpDataSource
    {
        public bool IsLazy => Container.IsLazy();

        internal MarshalContainer Container { get; private set; }

        public MarshalSource(HttpDataSource source)
        {
            Container = new MarshalContainer();
            Container.SetOrigin(source ?? throw new ArgumentNullException("source"));
        }

        public override long AproximateLength()
        {
            return Container.AproximateLength();
        }

        public override void Dispose()
        {
            Container.Dispose();
        }

        public override byte[] GetSourcePart(long start, long length)
        {
            return Container.GetSourcePart(start, length);
        }

        public override long ReadFromStream(Stream networkStream, long readlength)
        {
            return Container.ReadFromStream(networkStream, readlength);
        }

        public override long ReserveExtraMemory(long bytes)
        {
            return Container.ReserveExtraMemory(bytes);
        }

        public override int WriteSourcePart(byte[] source, long start, long length)
        {
            return Container.WriteSourcePart(source, start, length);
        }

        public override long WriteToStream(Stream networkStream)
        {
            return Container.WriteToStream(networkStream);
        }

        public override long RangeEnd
        {
            get => Container.RangeEnd();
            set => Container.RangeEnd(value);
        }

        public override long RangeStart
        {
            get => Container.RangeStart();
            set => Container.RangeStart(value);
        }

        public override bool TransferCompleteData
        {
            get => Container.TransferCompleteData();
            set => Container.TransferCompleteData(value);
        }

        public override string MimeType
        {
            get => Container.MimeType();
            set => Container.MimeType(value);
        }

        public Collections.MarshalEnumerable<HttpDataSource> GetAllSources()
        {
            return Container.Sources();
        }
    }

    internal class MarshalContainer : MarshalByRefObject
    {
        public HttpDataSource Origin { get; private set; }

        public void SetOrigin(HttpDataSource origin)
        {
            Origin = origin ?? throw new ArgumentNullException("origin");
        }
        
        public long AproximateLength()
        {
            return Origin.AproximateLength();
        }

        public void Dispose()
        {
            Origin.Dispose();
        }

        public byte[] GetSourcePart(long start, long length)
        {
            return Origin.GetSourcePart(start, length);
        }

        public long ReadFromStream(Stream networkStream, long readlength)
        {
            return Origin.ReadFromStream(networkStream, readlength);
        }

        public long ReserveExtraMemory(long bytes)
        {
            return Origin.ReserveExtraMemory(bytes);
        }

        public int WriteSourcePart(byte[] source, long start, long length)
        {
            return Origin.WriteSourcePart(source, start, length);
        }

        public long WriteToStream(Stream networkStream)
        {
            return Origin.WriteToStream(networkStream);
        }

        public long RangeEnd()
        {
            return Origin.RangeEnd;
        }

        public void RangeEnd(long value)
        {
            Origin.RangeEnd = value;
        }

        public long RangeStart()
        {
            return Origin.RangeStart;
        }

        public void RangeStart(long value)
        {
            Origin.RangeStart = value;
        }

        public bool TransferCompleteData()
        {
            return Origin.TransferCompleteData;
        }

        public void TransferCompleteData(bool value)
        {
            Origin.TransferCompleteData = value;
        }

        public string MimeType()
        {
            return Origin.MimeType;
        }

        public void MimeType(string value)
        {
            Origin.MimeType = value;
        }

        public bool IsLazy()
        {
            return Origin is Lazy.LazySource ||
                (Origin is MarshalSource ms && ms.IsLazy);
        }

        public Collections.MarshalEnumerable<HttpDataSource> Sources()
        {
            if (Origin is Lazy.LazySource source)
            {
                return new Collections.MarshalEnumerable<HttpDataSource>(
                    source.GetAllSources().Select((s) => new MarshalSource(s)));
            }
            else if (Origin is MarshalSource ms)
            {
                return ms.GetAllSources();
            }
            else
            {
                return null;
            }
        }
    }
}
