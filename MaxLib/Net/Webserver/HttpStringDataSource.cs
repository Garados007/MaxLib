using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MaxLib.Net.Webserver
{
    [Serializable]
    public class HttpStringDataSource : HttpDataSource
    {
        private string data = "";
        public string Data
        {
            get => data;
            set => data = value ?? throw new ArgumentNullException(nameof(Data));
        }

        private string encoding;
        public string TextEncoding
        {
            get => encoding;
            set
            {
                encoding = value;
                Encoder = Encoding.GetEncoding(value);
            }
        }

        public override bool CanAcceptData => true;

        public override bool CanProvideData => true;

        Encoding Encoder;

        public HttpStringDataSource(string data)
        {
            Data = data ?? throw new ArgumentNullException(nameof(data));
            Encoder = Encoding.UTF8;
            encoding = Encoder.WebName;
            TransferCompleteData = true;
        }

        public override void Dispose()
        {
        }

        public override long? Length()
            => Encoder.GetByteCount(Data);

        public override long WriteToStream(Stream networkStream)
        {
            var data = Encoder.GetBytes(Data);
            long length;
            try
            {
                networkStream.Write(data, (int)RangeStart,
                    (int)(length = (RangeEnd ?? data.Length) - RangeStart));
            }
            catch (IOException)
            {
                WebServerLog.Add(ServerLogType.Error, GetType(), "Send", "Connection closed by remote Host");
                return -1;
            }
            return length;
        }

        public override long ReadFromStream(Stream networkStream, long readlength)
        {
            var l = new List<byte>();
            var buffer = new byte[64 * 1024];
            long readed;
            do
            {
                var length = readlength == 0 ? buffer.Length : readlength - l.Count;
                readed = networkStream.Read(buffer, 0, (int)length);
                if (readed != 0)
                    l.AddRange(buffer.ToList().GetRange(0, (int)readed));
            }
            while (readed == 0);
            Data = Encoder.GetString(l.ToArray());
            return l.Count;
        }

        public override byte[] ReadSourcePart(long start, long length)
        {
            var b = Encoder.GetBytes(Data);
            return b.ToList().GetRange((int)start, (int)Math.Min(length, b.Length - start)).ToArray();
        }

        public override int WriteSourcePart(byte[] source, long start, long length)
        {
            var b = Encoder.GetBytes(Data);
            for (int i = 0; i < length; ++i) b[start + i] = source[i];
            Data = Encoder.GetString(b);
            return source.Length;
        }
    }
}
