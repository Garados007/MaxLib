using System;
using System.IO;

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

        public override long? Length()
        {
            return Container.Length();
        }

        public override void Dispose()
        {
            Container.Dispose();
        }

        public override byte[] ReadSourcePart(long start, long length)
        {
            return Container.GetSourcePart(start, length);
        }

        public override long ReadFromStream(Stream networkStream, long readlength)
        {
            return Container.ReadFromStream(networkStream, readlength);
        }

        public override int WriteSourcePart(byte[] source, long start, long length)
        {
            return Container.WriteSourcePart(source, start, length);
        }

        public override long WriteToStream(Stream networkStream)
        {
            return Container.WriteToStream(networkStream);
        }

        public override long? RangeEnd
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

        public override bool CanAcceptData => Container.CanAcceptData();

        public override bool CanProvideData => Container.CanProvideData();

        public Collections.MarshalEnumerable<HttpDataSource> GetAllSources()
        {
            return Container.Sources();
        }
    }
}
