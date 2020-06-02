using System;
using System.IO;

namespace MaxLib.Net.Webserver
{
    [Serializable]
    public class HttpStreamDataSource : HttpDataSource
    {
        public Stream Stream { get; set; }

        public bool ReadOnly { get; }

        public override bool CanAcceptData => true;

        public override bool CanProvideData => true;

        public HttpStreamDataSource(Stream stream, bool readOnly = true)
        {
            ReadOnly = readOnly;
            Stream = stream;
        }

        public override void Dispose()
            => Stream?.Dispose();

        public override long? Length()
            => Stream?.Length;

        public override long WriteToStream(Stream networkStream)
        {
            Stream.Position = TransferCompleteData ? 0 : RangeStart;
            var buffer = new byte[64 * 1024];
            long readed = 0;
            bool canRead;
            do
            {
                var length = RangeEnd == null
                    ? buffer.Length
                    : (int)Math.Min(buffer.Length, RangeEnd.Value - RangeStart - readed);
                var currentRead = Stream.Read(buffer, 0, length);
                readed += currentRead;
                try
                {
                    networkStream.Write(buffer, 0, length);
                }
                catch (IOException)
                {
                    WebServerLog.Add(ServerLogType.Error, GetType(), "Send", "Connection closed by remote Host");
                    return -1;
                }
                canRead = RangeEnd == null
                    ? currentRead > 0
                    : readed < RangeEnd.Value - RangeStart;
            }
            while (canRead);
            return readed;
        }

        public override long ReadFromStream(Stream networkStream, long readlength)
        {
            Stream.Position = 0;
            var buffer = new byte[64 * 1024];
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

        public override byte[] ReadSourcePart(long start, long length)
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
    }
}
