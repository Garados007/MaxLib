using MaxLib.Data;
using System;
using System.IO;
using System.Threading.Tasks;

namespace MaxLib.Net.Webserver
{
    [Serializable]
    public class HttpFileDataSource : HttpDataSource
    {
        public FileStream File { get; private set; }

        private string path = null;
        public virtual string Path
        {
            get => path;
            set
            {
                if (path == value) return;
                if (File != null) File.Dispose();
                if (value == null) File = null;
                else
                {
                    var fi = new FileInfo(value);
                    if (!fi.Directory.Exists) fi.Directory.Create();
                    File = new FileStream(value, FileMode.OpenOrCreate,
                        ReadOnly ? FileAccess.Read : FileAccess.ReadWrite, 
                        FileShare.ReadWrite);
                }
                path = value;
            }
        }

        public bool ReadOnly { get; }

        public override bool CanAcceptData => !ReadOnly;

        public override bool CanProvideData => true;

        public HttpFileDataSource(string path, bool readOnly = true)
        {
            ReadOnly = readOnly;
            Path = path;
        }

        public override void Dispose()
        {
            Path = null;
        }

        public override long? Length()
            => File?.Length;

        protected override async Task<long> WriteStreamInternal(Stream stream, long start, long? stop)
        {
            await Task.CompletedTask;
            File.Position = start;
            using (var skip = new SkipableStream(File, 0))
            {
                try
                {
                    return skip.WriteToStream(stream,
                        stop == null ? null : (long?)(stop.Value - start));
                }
                catch (IOException)
                {
                    WebServerLog.Add(ServerLogType.Information, GetType(), "Send", "Connection closed by remote Host");
                    return File.Position - start;
                }
            }
        }

        protected override async Task<long> ReadStreamInternal(Stream stream, long? length)
        {
            await Task.CompletedTask;
            if (ReadOnly)
                throw new NotSupportedException();
            File.Position = 0;
            using (var skip = new SkipableStream(File, 0))
            {
                long readed;
                try
                {
                    readed = skip.ReadFromStream(stream, length);
                }
                catch (IOException)
                {
                    WebServerLog.Add(ServerLogType.Information, GetType(), "Send", "Connection closed by remote Host");
                    readed = File.Position;
                }
                File.SetLength(readed);
                return readed;
            }
        }
    }
}
