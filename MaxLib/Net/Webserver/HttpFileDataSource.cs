using System;
using System.IO;

namespace MaxLib.Net.Webserver
{
    [Serializable]
    public class HttpFileDataSource : HttpDataSource
    {
        public System.IO.FileStream File { get; private set; }
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
                        ReadOnly ? FileAccess.Read : FileAccess.ReadWrite, ReadOnly ? FileShare.Read : FileShare.ReadWrite);
                }
                path = value;
            }
        }

        public bool ReadOnly { get; private set; }

        public HttpFileDataSource(string path, bool readOnly = true)
        {
            ReadOnly = readOnly;
            Path = path;
        }

        public override void Dispose()
        {
            Path = null;
        }

        public override long AproximateLength()
        {
            if (File == null) return 0;
            else return File.Length;
        }

        public override long WriteToStream(System.IO.Stream networkStream)
        {
            File.Position = TransferCompleteData ? 0 : RangeStart;
            var buffer = new byte[4 * 1024];
            long readed = 0;
            do
            {
                var length = (int)Math.Min(buffer.Length, TransferCompleteData ?
                    AproximateLength() - readed :
                    Math.Min(RangeEnd, AproximateLength()) - RangeStart - readed);
                readed += File.Read(buffer, 0, length);
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

        public override long ReadFromStream(System.IO.Stream networkStream, long readlength)
        {
            File.Position = 0;
            var buffer = new byte[4 * 1024];
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

        public override byte[] GetSourcePart(long start, long length)
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

        public override long ReserveExtraMemory(long bytes)
        {
            File.SetLength(File.Length + bytes);
            return bytes;
        }
    }
}
