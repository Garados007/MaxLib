using System;
using System.IO;

namespace MaxLib.Data.BitData
{
    public class BitSegmentWriter : IDisposable
    {
        private readonly bool disposeStream = true;
        private readonly BitSegment buffer = new BitSegment();

        public Stream BaseStream { get; }

        public BitSegmentWriter(Stream input)
            : this(input, false)
        {

        }

        public BitSegmentWriter(Stream input, bool leaveOpen)
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
            buffer.Clear();
        }

        public virtual void Close()
        {
            BaseStream.Close();
            buffer.Clear();
        }

        public bool HasUnflushedBits => buffer.Length > 0;

        protected virtual void FlushBuffer(bool force)
        {
            var maxBytes = buffer.Length >> 3;
            if (force && maxBytes * 8 < buffer.Length)
                maxBytes++;
            if (maxBytes == 0)
                return;
            //var bitsToHandle = Math.Min(buffer.Length, maxBytes * 8);
            //var flushBuffer = buffer
            //    .Section(0, bitsToHandle)
            //    .ToBytes(0, maxBytes);
            var flushBuffer = buffer.ToBytes(0, maxBytes);
            BaseStream.Write(flushBuffer, 0, maxBytes);
            buffer.TrimStart(maxBytes << 3);
        }

        public virtual void Flush()
        {
            FlushBuffer(true);
            BaseStream.Flush();
        }

        public virtual void WriteBits(Bits bits)
        {
            buffer.Append(ref bits);
            FlushBuffer(false);
        }
    }
}
