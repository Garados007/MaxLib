using System;
using System.IO;

namespace MaxLib.Data.Bits
{
    public class BitsWriter : IDisposable
    {
        private readonly bool disposeStream = true;
        private Bits buffer;

        public Stream BaseStream { get; private set; }

        public BitsWriter(Stream input)
            : this(input, false)
        {

        }

        public BitsWriter(Stream input, bool leaveOpen)
        {
            BaseStream = input ?? throw new ArgumentNullException(nameof(input));
            disposeStream = !leaveOpen;
            if (!BaseStream.CanWrite)
                throw new ArgumentException("this stream is not writeable", nameof(input));
        }

        public void Dispose()
        {
            if (disposeStream)
                BaseStream.Dispose();
            buffer = new Bits();
        }

        public virtual void Close()
        {
            BaseStream.Close();
            buffer = new Bits();
        }

        public bool HasUnflushedBits => buffer.Length > 0;

        protected virtual void FlushBuffer(bool force)
        {
            var maxBytes = buffer.Length / 8;
            if (force && maxBytes * 8 < buffer.Length)
                maxBytes++;
            if (maxBytes == 0)
                return;
            var flushBuffer = new byte[maxBytes];
            for (int i = 0; i<maxBytes; ++i)
            {
                flushBuffer[i] = buffer.ToByte(i * 8);
            }
            BaseStream.Write(flushBuffer, 0, maxBytes);
            buffer >>= Math.Min(buffer.Length, maxBytes * 8);
        }

        public virtual void Flush()
        {
            FlushBuffer(true);
            BaseStream.Flush();
        }

        public virtual void WriteBits(Bits bits)
        {
            buffer = Bits.Concat(buffer, bits);
            FlushBuffer(false);
        }
    }
}
