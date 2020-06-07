using MaxLib.Data;
using System;
using System.IO;
using System.Threading.Tasks;

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

        protected abstract long WriteStreamInternal(Stream stream, long start, long? stop);

        protected abstract long ReadStreamInternal(Stream stream, long? length);

        /// <summary>
        /// Write its content to <paramref name="stream"/>.
        /// </summary>
        /// <param name="stream">the stream to write the data into</param>
        /// <param name="start">the first own byte (inclusive) that should have been written</param>
        /// <param name="stop">the last own byte (excklusive) that should have been written or null to write all bytes till the end</param>
        /// <returns>the effective number of bytes written to the stream</returns>
        public long WriteStream(Stream stream, long start, long? stop)
        {
            _ = stream ?? throw new ArgumentNullException(nameof(stream));
            if (start < 0) throw new ArgumentOutOfRangeException(nameof(start));
            if (Length() != null && start >= Length().Value)
                throw new ArgumentOutOfRangeException(nameof(start));
            if (stop != null && stop < start) throw new ArgumentOutOfRangeException(nameof(stop));
            return WriteStreamInternal(stream, start, stop);
        }

        /// <summary>
        /// Write its content to <paramref name="stream"/>. It will start at <see cref="RangeStart"/> 
        /// (inclusive) and write until <see cref="RangeEnd"/> (exclusive). If <see cref="RangeEnd"/> 
        /// is null it will write all bytes till the end.
        /// </summary>
        /// <param name="stream">the stream to write the data into</param>
        /// <returns>the effective number of bytes written to the stream</returns>
        public long WriteStream(Stream stream)
            => WriteStream(stream ?? throw new ArgumentNullException(nameof(stream)), RangeStart, RangeEnd);

        /// <summary>
        /// Read the data of <paramref name="stream"/> and replace its own data with it.
        /// </summary>
        /// <param name="stream">the stream to read the data from</param>
        /// <param name="length">the number of bytes that should been readed. null to read all bytes.</param>
        /// <returns>the number of bytes readed from the stream</returns>
        public long ReadStream(Stream stream, long? length = null)
        {
            _ = stream ?? throw new ArgumentNullException(nameof(stream));
            if (length != null && length < 0) throw new ArgumentOutOfRangeException(nameof(length));
            return ReadStreamInternal(stream, length);
        }

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

        public static Stream TransformToStream(HttpDataSource dataSource)
        {
            _ = dataSource ?? throw new ArgumentNullException(nameof(dataSource));
            var buffered = new BufferedSinkStream();
            _ = new Task(() =>
            {
                dataSource.WriteStream(buffered);
                buffered.FinishWrite();
            });
            return buffered;
        }
    }
}
