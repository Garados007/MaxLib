using System;
using System.IO;

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

        public override bool CanAcceptData => true;

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

        public override long WriteToStream(Stream networkStream)
        {
            File.Position = TransferCompleteData ? 0 : RangeStart;
            var buffer = new byte[64 * 1024];
            long readed = 0;
            bool hasmore;
            var end = RangeEnd ?? Length();
            do
            {
                var length = end == null 
                    ? buffer.Length 
                    : (int)Math.Min(buffer.Length, end.Value - RangeStart - readed);
                var currentRead = File.Read(buffer, 0, length);
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
                hasmore = end == null
                    ? currentRead > 0
                    : readed < end.Value - RangeStart;
            }
            while (hasmore);
            return readed;
        }

        public override long ReadFromStream(Stream networkStream, long readlength)
        {
            File.Position = 0;
            var buffer = new byte[64 * 1024];
            long readed = 0;
            int r;
            do
            {
                var length = readlength == 0 ? buffer.Length :
                    (int)Math.Min(buffer.Length, readlength - readed);
                r = networkStream.Read(buffer, 0, length);
                File.Write(buffer, 0, r);
                readed += r;
            }
            while (r != 0);
            File.SetLength(readed);
            return readed;
        }

        public override byte[] ReadSourcePart(long start, long length)
        {
            var b = new byte[length];
            File.Position = start;
            File.Read(b, 0, b.Length);
            return b;
        }

        public override int WriteSourcePart(byte[] source, long start, long length)
        {
            File.Position = start;
            File.Write(source, 0, (int)length);
            return (int)length;
        }
    }
}
