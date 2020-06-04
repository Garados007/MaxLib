using System;
using System.IO;

namespace MaxLib.Net.Webserver
{
    [Serializable]
    public class HttpStreamDataSource : HttpDataSource
    {
        public Stream Stream { get; set; }

        public bool ReadOnly { get; }

        public override bool CanAcceptData => !ReadOnly;

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

        protected override long WriteStreamInternal(Stream stream, long start, long? stop)
        {
            Stream.Position = start;
            using (var skip = new SkipableStream(Stream, 0))
            {
                try
                {
                    return skip.WriteToStream(stream, 
                        stop == null ? null : (long?)(stop.Value - start));
                }
                catch (IOException)
                {
                    WebServerLog.Add(ServerLogType.Information, GetType(), "Send", "Connection closed by remote Host");
                    return Stream.Position - start;
                }
            }
        }

        protected override long ReadStreamInternal(Stream stream, long? length)
        {
            if (ReadOnly)
                throw new NotSupportedException();
            Stream.Position = 0;
            using (var skip = new SkipableStream(Stream, 0))
            {
                long readed;
                try
                {
                    readed = skip.ReadFromStream(stream, length);
                }
                catch (IOException)
                {
                    WebServerLog.Add(ServerLogType.Information, GetType(), "Send", "Connection closed by remote Host");
                    readed = Stream.Position;
                }
                Stream.SetLength(readed);
                return readed;
            }
        }
    }
}
