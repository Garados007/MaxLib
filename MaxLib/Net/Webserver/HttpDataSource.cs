using System;

namespace MaxLib.Net.Webserver
{
    [Serializable]
    public abstract class HttpDataSource : IDisposable
    {
        public abstract void Dispose();

        public abstract long AproximateLength();

        private string mimeType = Webserver.MimeType.TextHtml;
        public virtual string MimeType
        {
            get => mimeType;
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    value = "text/html";
                mimeType = value;
            }
        }

        public abstract long WriteToStream(System.IO.Stream networkStream);

        public abstract long ReadFromStream(System.IO.Stream networkStream, long readlength);

        public bool NeedBufferManagement { get; protected set; }

        public abstract byte[] GetSourcePart(long start, long length);

        public abstract int WriteSourcePart(byte[] source, long start, long length);

        public abstract long ReserveExtraMemory(long bytes);

        private long rangeStart;
        public virtual long RangeStart
        {
            get
            {
                if (TransferCompleteData) rangeStart = 0;
                return rangeStart;
            }
            set
            {
                if (value < 0) throw new ArgumentOutOfRangeException("RangeStart");
                if (value > 0) TransferCompleteData = false;
                else if (rangeEnd == AproximateLength() - 1) TransferCompleteData = true;
                rangeStart = value;
            }
        }

        private long rangeEnd;
        public virtual long RangeEnd
        {
            get
            {
                if (TransferCompleteData) rangeEnd = AproximateLength() - 1;
                return rangeEnd;
            }
            set
            {
                var length = AproximateLength();
                if (value != length - 1) TransferCompleteData = false;
                else if (rangeStart == 0) TransferCompleteData = true;
                rangeEnd = value;
            }
        }

        private bool transferCompleteData;
        public virtual bool TransferCompleteData
        {
            get => transferCompleteData;
            set
            {
                if (transferCompleteData = value)
                {
                    rangeStart = 0;
                    rangeEnd = AproximateLength();
                }
            }
        }
    }
}
