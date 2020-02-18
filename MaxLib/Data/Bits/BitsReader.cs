using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace MaxLib.Data.Bits
{
    public class BitsReader : IDisposable
    {
        private readonly bool disposeStream = true;
        private bool endReached = false;
        private Bits buffer;

        public Stream BaseStream { get; private set; }

        public BitsReader(Stream input)
            : this(input, false)
        {
        }

        public BitsReader(Stream input, bool leaveOpen)
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
            buffer = new Bits();
        }

        public virtual void Close()
        {
            BaseStream.Close();
            buffer = new Bits();
        }

        protected virtual void FillBuffer(int numBytes)
        {
            if (numBytes < 0) throw new ArgumentOutOfRangeException(nameof(numBytes));
            var result = new byte[numBytes];
            var readed = BaseStream.Read(result, 0, numBytes);
            endReached |= readed == 0;
            var array = new byte[readed];
            for (int i = 0; i < readed; ++i)
                array[i] = result[i];
            buffer = Bits.Concat(buffer, array);
        }

        public bool EndOfStream => endReached && buffer.Length == 0;

        public virtual Bits ReadBits(int count)
        {
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
            //check cache
            if (buffer.Length < count)
            {
                FillBuffer((count - buffer.Length) / 8 + 1);
            }
            //get portion
            var portion = buffer.ToBits(0, Math.Min(count, buffer.Length));
            buffer >>= portion.Length;
            //extend portion to match requested count
            var extend = new Bit[count - portion.Length];
            return Bits.Concat(portion, extend);
        }
    }
}
