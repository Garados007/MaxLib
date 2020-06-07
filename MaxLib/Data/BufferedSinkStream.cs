using System;
using System.IO;
using System.Threading;

namespace MaxLib.Data
{
    /// <summary>
    /// This provides a small buffer. On the one end you can push data in and on the other end pull them out.
    /// If the buffer is full a write request is blocked. The same is for empty buffer and read requests.
    /// </summary>
    public class BufferedSinkStream : Stream
    {
        readonly byte[] buffer;

        long readTotal = 0;
        long writeTotal = 0;

        int readOffset = 0;
        int writeOffset = 0;

        readonly object checkOffsetLock = new object();
        readonly Semaphore readLock;
        readonly Semaphore writeLock;

        public BufferedSinkStream(int length)
        {
            if (length <= 0)
                throw new ArgumentOutOfRangeException(nameof(length));
            buffer = new byte[length];
            readLock = new Semaphore(0, 1);
            writeLock = new Semaphore(1, 1);
        }

        /// <summary>
        /// Creates a new <see cref="BufferedSinkStream"/> with 1 MB buffer
        /// </summary>
        public BufferedSinkStream()
            : this(1 << 20)
        { }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            readLock.Dispose();
            writeLock.Dispose();
        }

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public override long Length
            => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush()
            => throw new NotSupportedException();

        public override int Read(byte[] buffer, int offset, int count)
        {
            _ = buffer ?? throw new ArgumentNullException(nameof(buffer));
            if (offset < 0 || offset > buffer.Length)
                throw new ArgumentOutOfRangeException(nameof(offset));
            if (count < 0 || offset + count > buffer.Length)
                throw new ArgumentOutOfRangeException(nameof(count));
            int origOff = offset;
            while (count > 0)
            {
                var part = Math.Min(count, buffer.Length - readOffset);
                if (part == 0)
                {
                    readOffset = 0;
                    continue;
                }
                var readed = ReadPart(buffer, offset, part);
                offset += readed;
                count -= readed;
            }
            return offset - origOff;
        }

        private int ReadPart(byte[] buffer, int offset, int count)
        {
            readLock.WaitOne();
            long diff;
            lock (checkOffsetLock)
                diff = writeTotal - readTotal;
            var copyMax = (int)Math.Min(count, diff);
            Array.Copy(this.buffer, readOffset, buffer, offset, copyMax);
            readOffset += copyMax;
            lock (checkOffsetLock)
            {
                readTotal += copyMax;
                diff = writeTotal - readTotal;
                if (diff > 0)
                    readLock.Release();
                if (diff < buffer.Length)
                    writeLock.Release();
            }
            return copyMax;
        }

        public override long Seek(long offset, SeekOrigin origin)
            => throw new NotSupportedException();

        public override void SetLength(long value)
            => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count)
        {
            _ = buffer ?? throw new ArgumentNullException(nameof(buffer));
            if (offset < 0 || offset > buffer.Length)
                throw new ArgumentOutOfRangeException(nameof(offset));
            if (count < 0 || offset + count > buffer.Length)
                throw new ArgumentOutOfRangeException(nameof(count));
            while (count > 0)
            {
                var part = Math.Min(count, buffer.Length - writeOffset);
                if (part == 0)
                {
                    writeOffset = 0;
                    continue;
                }
                var written = WritePart(buffer, offset, part);
                offset += written;
                count -= written;
            }
        }

        private int WritePart(byte[] buffer, int offset, int count)
        {
            writeLock.WaitOne();
            long diff;
            lock (checkOffsetLock)
                diff = writeTotal - readTotal;
            var copyMax = (int)Math.Min(count, this.buffer.Length - diff);
            Array.Copy(buffer, offset, this.buffer, writeOffset, copyMax);
            writeOffset += copyMax;
            lock (checkOffsetLock)
            {
                writeTotal += copyMax;
                diff = writeTotal - readTotal;
                if (diff > 0)
                    readLock.Release();
                if (diff < buffer.Length)
                    writeLock.Release();
                if (writeTotal > long.MaxValue >> 1)
                {
                    writeTotal -= long.MaxValue >> 1;
                    readTotal -= long.MaxValue >> 1;
                }
            }
            return copyMax;
        }
    }
}
