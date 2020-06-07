using System;
using System.IO;

namespace MaxLib.Net.Webserver.Remote
{
    [Serializable]
    public class MarshalSource : HttpDataSource
    {
        public bool IsLazy => Container.IsLazy();

        internal MarshalContainer Container { get; }

        public MarshalSource(HttpDataSource source)
        {
            Container = new MarshalContainer();
            Container.SetOrigin(source ?? throw new ArgumentNullException(nameof(source)));
        }

        public override long? Length()
            => Container.Length();

        public override void Dispose()
            => Container.Dispose();

        protected override long WriteStreamInternal(Stream stream, long start, long? stop)
            => Container.WriteStream(stream, start, stop);

        protected override long ReadStreamInternal(Stream stream, long? length)
            => Container.ReadStream(stream, length);

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
            => Container.Sources();
    }
}
