using System;
using System.IO;

namespace MaxLib.Data.BitData
{
    public class BitSegmentReader : IDisposable
    {
        private readonly bool disposeStream = true;
        private bool endReached = false;
        private readonly BitSegment buffer = new BitSegment();

        public Stream BaseStream { get; }

        public BitSegmentReader(Stream input)
            : this(input, false)
        {
        }

        public BitSegmentReader(Stream input, bool leaveOpen)
        {
            BaseStream = input ?? throw new ArgumentNullException(nameof(input));
            disposeStream = !leaveOpen;
            if (!BaseStream.CanRead)
                throw new ArgumentException("this stream is not readable", nameof(input));
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

        protected virtual void FillBuffer(int numBytes)
        {
            if (numBytes < 0) 
                throw new ArgumentOutOfRangeException(nameof(numBytes));
            var result = new byte[numBytes];
            var readed = BaseStream.Read(result, 0, numBytes);
            endReached |= readed == 0;
            buffer.Append(BitSegment.ToBits(result, 0, readed));
        }

        public bool EndOfStream => endReached && buffer.Length == 0;

        public virtual BitSegment ReadBitSegment(int count)
        {
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
            //check cache
            if (buffer.Length < count)
            {
                FillBuffer(((count - buffer.Length) >> 3) + 1);
            }
            //get portion
            var portion = buffer.SegmentSection(0, Math.Min(count, buffer.Length));
            buffer.TrimStart(portion.Length);
            //extend portion to match requested count
            if (count > portion.Length)
                portion.Append(new Bit[count - portion.Length]);
            return portion;
        }
    }
}
