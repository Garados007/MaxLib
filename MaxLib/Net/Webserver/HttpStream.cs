using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MaxLib.Net.Webserver
{
    public class HttpStream : Stream
    {
        public Stream BaseStream { get; }

        public Encoding Encoding { get; }

        private readonly byte[] buffer;
        private int bufferLength = 0;
        private int bufferOffset = 0;
        private readonly object bufferLock = new object();

        public override bool CanRead => BaseStream.CanRead;

        public override bool CanSeek => false;

        public override bool CanWrite => BaseStream.CanWrite;

        public override long Length
            => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public HttpStream(Stream baseStream)
            : this(baseStream, Encoding.Default)
        { }

        public HttpStream(Stream baseStream, Encoding encoding)
            : this(baseStream, encoding, 0x800)
        { }

        public HttpStream(Stream baseStream, Encoding encoding, int bufferSize)
        {
            BaseStream = baseStream ?? throw new ArgumentNullException(nameof(baseStream));
            Encoding = encoding ?? throw new ArgumentNullException(nameof(encoding));
            if (bufferSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(bufferSize));
            if (bufferSize < Encoding.GetMaxByteCount(1))
                throw new ArgumentOutOfRangeException(nameof(bufferSize), $"buffer needs to be at least {Encoding.GetMaxByteCount(1)} bytes");
            buffer = new byte[bufferSize];
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            BaseStream.Dispose();
        }

        public override void Flush()
            => BaseStream.Flush();

        public override int Read(byte[] buffer, int offset, int count)
        {
            _ = buffer ?? throw new ArgumentNullException(nameof(buffer));
            if (offset < 0 || offset > buffer.Length)
                throw new ArgumentOutOfRangeException(nameof(offset));
            if (count < 0 || offset + count > buffer.Length)
                throw new ArgumentOutOfRangeException(nameof(count));
            if (count == 0)
                return 0;
            int readed = ReadFromBuffer(buffer, offset, count);
            if (readed == count)
                return readed;
            else return readed + BaseStream.Read(buffer, offset + readed, count - readed);
        }

        private int ReadFromBuffer(byte[] buffer, int offset, int count)
        {
            if (count == 0)
                return 0;
            int start, end;
            lock (bufferLock)
            {
                if (bufferLength == 0)
                    return 0;
                start = bufferOffset;
                end = Math.Min(this.buffer.Length, bufferOffset + bufferLength);
            }
            if (start == end)
            {
                lock (bufferLock)
                    bufferOffset = 0;
                return ReadFromBuffer(buffer, offset, count);
            }
            int readed = Math.Min(count, end - start);
            lock (bufferLock)
            {
                bufferOffset += readed;
                bufferLength -= readed;
            }
            Array.Copy(this.buffer, start, buffer, offset, readed);
            if (readed < count)
                readed += ReadFromBuffer(buffer, offset + readed, count - readed);
            return readed;
        }

        public string ReadLine()
        {
            var sb = new StringBuilder();
            int maxCharLength = Encoding.GetMaxByteCount(1);
            byte[] single = new byte[maxCharLength];
            bool atEnd = false;
            var preample = Encoding.GetPreamble();
            int preampleIndex = 0;
            while (true)
            {
                bool hasBuffer;
                lock (bufferLock)
                    hasBuffer = bufferLength > 0;
                if (!hasBuffer && !ReadBlockToBuffer())
                    return sb.Length == 0 ? null : sb.ToString();
                hasBuffer = true;

                int length;
                lock (bufferLock)
                    length = bufferLength;
                if (length < maxCharLength)
                    ReadBlockToBuffer();

                FillSingleCharBuffer(single);
                var text = Encoding.GetString(single);
                if (text.Length == 0)
                    return sb.ToString();
                var first = text[0];
                var firstLength = Encoding.GetByteCount(new[] { first });

                bool hasPreample = false;
                if (preampleIndex < preample.Length)
                {
                    hasPreample = true;
                    for (int i = 0; i < preample.Length - preampleIndex && i < firstLength; ++i)
                        if (single[i] != preample[i + preampleIndex])
                            hasPreample = false;
                }

                if (hasPreample)
                    preampleIndex += firstLength;
                else
                    switch (first)
                    {
                        case '\r':
                            atEnd = true;
                            break;
                        case '\n':
                            hasBuffer = false;
                            break;
                        default:
                            if (atEnd)
                                return sb.ToString();
                            else sb.Append(first);
                            break;
                    }

                lock (bufferLock)
                {
                    bufferOffset += firstLength;
                    if (bufferOffset >= buffer.Length)
                        bufferOffset -= buffer.Length;
                    bufferLength -= firstLength;
                }

                if (!hasBuffer)
                    return sb.ToString();
            }
        }

        public Task<string> ReadLineAsync()
        {
            return ReadLineAsync(CancellationToken.None);
        }

        public Task<string> ReadLineAsync(CancellationToken cancellationToken)
        {
            return Task.Factory.StartNew(state => ((HttpStream)state).ReadLine(), this, cancellationToken, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
        }

        private void FillSingleCharBuffer(byte[] buffer)
        {
            lock (bufferLock)
            {
                for (int i = 0, ind = bufferOffset; i<buffer.Length && i < bufferLength; ++i, ++ind)
                {
                    if (ind >= this.buffer.Length)
                        ind -= this.buffer.Length;
                    buffer[i] = this.buffer[ind];
                }
            }
        }

        private bool ReadBlockToBuffer()
        {
            int start, end, length;
            lock (bufferLock) 
            {
                //position to start the read
                start = (bufferOffset + bufferLength) % buffer.Length;
                //maximum number of bytes that can filled in the buffer
                length = buffer.Length - bufferLength;
                //the highest position that can be written from start in the current loop
                end = Math.Min(start + length, buffer.Length);
            }
            int readed = BaseStream.Read(buffer, start, end - start);
            lock (bufferLock) 
                bufferLength += readed;
            return readed > 0;
        }

        public override long Seek(long offset, SeekOrigin origin)
            => throw new NotSupportedException();

        public override void SetLength(long value)
            => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count)
            => BaseStream.Write(buffer, offset, count);

        public void WriteLine(string text)
        {
            _ = text ?? throw new ArgumentNullException(nameof(text));
            var buffer = Encoding.GetBytes(text);
            Write(buffer, 0, buffer.Length);
            buffer = Encoding.GetBytes("\r\n");
            Write(buffer, 0, buffer.Length);
        }

        public Task WriteAsync(string text)
        {
            return WriteAsync(text, CancellationToken.None);
        }

        public Task WriteAsync(string text, CancellationToken cancellationToken)
        {
            return Task.Factory.StartNew(state => ((HttpStream)state).WriteLine(text), this, cancellationToken, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
        }
    }
}
