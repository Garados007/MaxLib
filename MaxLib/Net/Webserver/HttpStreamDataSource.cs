using MaxLib.Data;
using System;
using System.IO;
using System.Threading.Tasks;

namespace MaxLib.Net.Webserver
{
    [Serializable]
    public class HttpStreamDataSource : HttpDataSource
    {
        public Stream Stream { get; }

        public bool ReadOnly { get; }

        public override bool CanAcceptData => !ReadOnly && Stream.CanWrite;

        public override bool CanProvideData => Stream.CanRead;

        public HttpStreamDataSource(Stream stream, bool readOnly = true)
        {
            ReadOnly = readOnly;
            Stream = stream ?? throw new ArgumentNullException(nameof(stream));
        }

        public override void Dispose()
            => Stream?.Dispose();

        public override long? Length()
            => Stream?.Length;

        protected override async Task<long> WriteStreamInternal(Stream stream, long start, long? stop)
        {
            await Task.CompletedTask;
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

        protected override async Task<long> ReadStreamInternal(Stream stream, long? length)
        {
            await Task.CompletedTask;
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
