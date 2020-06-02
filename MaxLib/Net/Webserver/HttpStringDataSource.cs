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
            set => data = value ?? throw new ArgumentNullException("Data");
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

        Encoding Encoder;

        public HttpStringDataSource(string data)
        {
            Data = data ?? throw new ArgumentNullException("data");
            NeedBufferManagement = false;
            Encoder = Encoding.UTF8;
            encoding = Encoder.WebName;
            TransferCompleteData = true;
        }

        public override void Dispose()
        {
        }

        public override long AproximateLength()
        {
            return Encoder.GetByteCount(Data);
        }

        public override long WriteToStream(System.IO.Stream networkStream)
        {
            var data = Encoder.GetBytes(Data);
            long length;
            try
            {
                if (TransferCompleteData) networkStream.Write(data, 0, (int)(length = data.Length));
                else networkStream.Write(data, (int)RangeStart,
                    (int)(length = Math.Min(RangeEnd, data.Length) - RangeStart));
            }
            catch (IOException)
            {
                WebServerLog.Add(ServerLogType.Error, GetType(), "Send", "Connection closed by remote Host");
                return -1;
            }
            return length;
        }

        public override long ReadFromStream(System.IO.Stream networkStream, long readlength)
        {
            var l = new List<byte>();
            var buffer = new byte[4 * 1024];
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

        public override byte[] GetSourcePart(long start, long length)
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

        public override long ReserveExtraMemory(long bytes)
        {
            return 0; //Nicht notwendig
        }
    }
}
