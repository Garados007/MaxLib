using System;
using System.IO;

namespace MaxLib.Net.Webserver
{
    /// <summary>
    /// This stream reads/writes the data of/to <see cref="BaseStream"/> and skip the first
    /// <see cref="SkipBytes"/>. These bytes are completly ignored and discarded.
    /// </summary>
    public class SkipableStream : Stream
    {
        public Stream BaseStream { get; }

        public long SkipBytes { get; private set; }

        public SkipableStream(Stream baseStream, long skipBytes)
        {
            BaseStream = baseStream ?? throw new ArgumentNullException(nameof(baseStream));
            if (skipBytes < 0)
                throw new ArgumentOutOfRangeException(nameof(skipBytes));
            SkipBytes = skipBytes;
        }

        public override bool CanRead => BaseStream.CanRead;

        public override bool CanSeek => false;

        public override bool CanWrite => BaseStream.CanWrite;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush()
            => BaseStream.Flush();

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (!CanRead)
                throw new NotSupportedException();
            if (SkipBytes > 0)
            {
                var skip = new byte[0x10000];
                while (SkipBytes > 0)
                {
                    var readed = BaseStream.Read(
                        skip, 
                        0, 
                        (int)Math.Min(skip.Length, SkipBytes)
                        );
                    if (readed == 0)
                        return 0;
                    SkipBytes -= readed;
                }
            }
            return BaseStream.Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
            => throw new NotSupportedException();

        public override void SetLength(long value)
            => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (!CanWrite)
                throw new NotSupportedException();
            if (SkipBytes >= count)
            {
                SkipBytes -= count;
                return;
            }
            var diff = (int)Math.Min(count, SkipBytes);
            SkipBytes -= diff;
            BaseStream.Write(buffer, offset + diff, count - diff);
        }

        /// <summary>
        /// This will read the contents of this stream and write them to
        /// <paramref name="stream"/>. <paramref name="count"/> includes
        /// the first <see cref="SkipBytes"/>!
        /// </summary>
        /// <param name="stream">the stream to write the content into</param>
        /// <param name="count">number of bytes to read or null to read the whole content</param>
        /// <returns>the number of bytes written to the <paramref name="stream"/></returns>
        public virtual long WriteToStream(Stream stream, long? count = null)
        {
            if (!CanRead)
                throw new NotSupportedException();
            _ = stream ?? throw new ArgumentNullException(nameof(stream));
            if (!stream.CanWrite)
                throw new ArgumentException("stream is not writable", nameof(stream));
            if (count != null && count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));

            if (count != null)
                count -= SkipBytes;
            long readed = 0;
            int currentRead;
            var buffer = new byte[0x10000];
            do
            {
                var length = count == null
                    ? buffer.Length
                    : (int)Math.Min(buffer.Length, count.Value - readed);
                currentRead = Read(buffer, 0, length);
                stream.Write(buffer, 0, currentRead);
                readed += currentRead;
            }
            while (currentRead > 0);
            return readed;
        }

        /// <summary>
        /// This will read the contents of <paramref name="stream"/> and
        /// write it to this stream. <paramref name="count"/> includes the
        /// first <see cref="SkipBytes"/>!
        /// </summary>
        /// <param name="stream">the stream to read the data from</param>
        /// <param name="count">the number of bytes to write or null to write to whole content</param>
        /// <returns>the number of bytes written to <see cref="BaseStream"/></returns>
        public virtual long ReadFromStream(Stream stream, long? count = null)
        {
            if (!CanWrite)
                throw new NotSupportedException();
            _ = stream ?? throw new ArgumentNullException(nameof(stream));
            if (!stream.CanRead)
                throw new ArgumentException("stream is not readable", nameof(stream));
            if (count != null && count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));

            long skip = SkipBytes;
            long readed = 0;
            int currentRead;
            var buffer = new byte[0x10000];
            do
            {
                var length = count == null
                    ? buffer.Length
                    : (int)Math.Min(buffer.Length, count.Value - readed);
                currentRead = stream.Read(buffer, 0, length);
                Write(buffer, 0, currentRead);
                readed += currentRead;
            }
            while (currentRead > 0);
            return Math.Max(0, readed - skip);
        }

        /// <summary>
        /// This will reduce the amount of to skipping bytes with 
        /// <paramref name="skipBytes"/>
        /// </summary>
        /// <param name="skipBytes">the amount of bytes that was skipped</param>
        public void Skip(long skipBytes)
        {
            if (skipBytes < 0 || skipBytes > SkipBytes)
                throw new ArgumentOutOfRangeException(nameof(skipBytes));
            SkipBytes -= skipBytes;
        }
    }
}
