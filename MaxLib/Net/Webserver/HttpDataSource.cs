using System;
using System.IO;

namespace MaxLib.Net.Webserver
{
    [Serializable]
    public abstract class HttpDataSource : IDisposable
    {
        public abstract void Dispose();

        public abstract long? Length();

        private string mimeType = Webserver.MimeType.TextHtml;
        public virtual string MimeType
        {
            get => mimeType;
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    mimeType = Webserver.MimeType.TextHtml;
                else mimeType = value;
            }
        }

        public abstract bool CanAcceptData { get; }

        public abstract bool CanProvideData { get; }

        public abstract long WriteToStream(Stream networkStream);

        public abstract long ReadFromStream(Stream networkStream, long readlength);

        public abstract byte[] ReadSourcePart(long start, long length);

        public abstract int WriteSourcePart(byte[] source, long start, long length);

        private long rangeStart = 0;
        public virtual long RangeStart
        {
            get => rangeStart;
            set
            {
                if (value < 0) throw new ArgumentOutOfRangeException(nameof(RangeStart));
                if (value > 0) TransferCompleteData = false;
                else if (rangeEnd == null) TransferCompleteData = true;
                rangeStart = value;
            }
        }

        private long? rangeEnd = null;
        public virtual long? RangeEnd
        {
            get => rangeEnd;
            set
            {
                if (Length() == null && value != null)
                    throw new ArgumentOutOfRangeException(nameof(RangeEnd));
                if (value != null && value < 0) throw new ArgumentOutOfRangeException(nameof(RangeEnd));
                if (value != null)
                    TransferCompleteData = false;
                else if (rangeStart == 0) 
                    TransferCompleteData = true;
                rangeEnd = value;
            }
        }

        private bool transferCompleteData = true;
        public virtual bool TransferCompleteData
        {
            get => transferCompleteData;
            set
            {
                if (transferCompleteData = value)
                {
                    rangeStart = 0;
                    rangeEnd = null;
                }
                else
                {
                    if (rangeEnd == null)
                        rangeEnd = Length();
                }
            }
        }
    }
}
