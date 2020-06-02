using System;
using System.IO;
using System.Text;

namespace MaxLib.Net.Webserver.Chunked
{
    [Serializable]
    public class HttpChunkedStream : HttpDataSource
    {
        public HttpChunkedStream(Stream baseStream, int readBufferLength = 0x8000)
        {
            BaseStream = baseStream ?? throw new ArgumentNullException("baseStream");
            ReadBufferLength = readBufferLength;
            if (readBufferLength <= 0) throw new ArgumentOutOfRangeException("readBufferLength");
        }

        public Stream BaseStream { get; private set; }

        public int ReadBufferLength { get; private set; }

        public override long AproximateLength()
        {
            throw new NotSupportedException();
        }

        public override void Dispose()
        {
            BaseStream.Dispose();
        }

        public override byte[] GetSourcePart(long start, long length)
        {
            throw new NotSupportedException();
        }

        public override long ReadFromStream(Stream networkStream, long readlength)
        {
            throw new NotSupportedException();
        }

        public override long ReserveExtraMemory(long bytes)
        {
            throw new NotSupportedException();
        }

        public override int WriteSourcePart(byte[] source, long start, long length)
        {
            throw new NotSupportedException();
        }

        public override long WriteToStream(Stream networkStream)
        {
            long total = 0;
            int readed;
            byte[] buffer = new byte[ReadBufferLength];
            var ascii = Encoding.ASCII;
            var nl = ascii.GetBytes("\r\n");
            while ((readed = BaseStream.Read(buffer, 0, buffer.Length)) > 0)
            {
                var length = ascii.GetBytes(readed.ToString("X"));
                networkStream.Write(length, 0, length.Length);
                networkStream.Write(nl, 0, nl.Length);
                networkStream.Write(buffer, 0, readed);
                networkStream.Write(nl, 0, nl.Length);
                total += readed;
                networkStream.Flush();
            }
            return total;
        }
    }
}
