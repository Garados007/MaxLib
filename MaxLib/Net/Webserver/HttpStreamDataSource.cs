using System;
using System.IO;

namespace MaxLib.Net.Webserver
{
    [Serializable]
    public class HttpStreamDataSource : HttpDataSource
    {
        public Stream Stream { get; set; }

        public bool ReadOnly { get; private set; }

        public HttpStreamDataSource(Stream stream, bool readOnly = true)
        {
            ReadOnly = readOnly;
            Stream = stream;
        }

        public override void Dispose()
        {
            Stream?.Dispose();
        }

        public override long AproximateLength()
        {
            if (Stream == null) return 0;
            else return Stream.Length;
        }

        public override long WriteToStream(Stream networkStream)
        {
            Stream.Position = TransferCompleteData ? 0 : RangeStart;
            var buffer = new byte[4 * 1024];
            long readed = 0;
            do
            {
                var length = (int)Math.Min(buffer.Length, TransferCompleteData ?
                    AproximateLength() - readed :
                    Math.Min(RangeEnd, AproximateLength()) - RangeStart - readed);
                readed += Stream.Read(buffer, 0, length);
                try
                {
                    networkStream.Write(buffer, 0, length);
                }
                catch (IOException)
                {
                    WebServerInfo.Add(InfoType.Error, GetType(), "Send", "Connection closed by remote Host");
                    return -1;
                }
            }
            while (readed != (TransferCompleteData ? AproximateLength() :
                Math.Min(RangeEnd, AproximateLength()) - RangeStart));
            return readed;
        }

        public override long ReadFromStream(Stream networkStream, long readlength)
        {
            Stream.Position = 0;
            var buffer = new byte[4 * 1024];
            long readed = 0;
            int r;
            do
            {
                var length = readlength == 0 ? buffer.Length :
                    (int)Math.Min(buffer.Length, readlength - readed);
                r = networkStream.Read(buffer, 0, length);
                Stream.Write(buffer, 0, r);
                readed += r;
            }
            while (r != 0);
            Stream.SetLength(readed);
            return readed;
        }

        public override byte[] GetSourcePart(long start, long length)
        {
            var b = new byte[length];
            Stream.Position = start;
            Stream.Read(b, 0, b.Length);
            return b;
        }

        public override int WriteSourcePart(byte[] source, long start, long length)
        {
            Stream.Position = start;
            Stream.Write(source, 0, (int)length);
            return (int)length;
        }

        public override long ReserveExtraMemory(long bytes)
        {
            Stream.SetLength(Stream.Length + bytes);
            return bytes;
        }
    }
}
