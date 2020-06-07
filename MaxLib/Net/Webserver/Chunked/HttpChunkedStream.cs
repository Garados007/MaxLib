using System;
using System.IO;
using System.Text;
using System.Globalization;
using System.Threading.Tasks;

namespace MaxLib.Net.Webserver.Chunked
{
    [Serializable]
    public class HttpChunkedStream : HttpDataSource
    {
        public HttpChunkedStream(Stream baseStream, int readBufferLength = 0x8000)
        {
            BaseStream = baseStream ?? throw new ArgumentNullException(nameof(baseStream));
            if (readBufferLength <= 0) 
                throw new ArgumentOutOfRangeException(nameof(readBufferLength));
            ReadBufferLength = readBufferLength;
        }

        public Stream BaseStream { get; }

        public int ReadBufferLength { get; }

        public override bool CanAcceptData => BaseStream.CanWrite;

        public override bool CanProvideData => BaseStream.CanRead;

        public override long? Length() => null;

        public override void Dispose()
        {
            BaseStream.Dispose();
        }

        protected override async Task<long> WriteStreamInternal(Stream stream, long start, long? stop)
        {
            long total = 0;
            int readed;
            byte[] buffer = new byte[ReadBufferLength];
            do
            {
                readed = await BaseStream.ReadAsync(buffer, 0, (int)Math.Min(buffer.Length, start - total));
                total += readed;
            }
            while (total < start && readed > 0);
            if (readed == 0)
                return 0;
            var ascii = Encoding.ASCII;
            var nl = ascii.GetBytes("\r\n");
            do
            {
                var read = stop == null
                    ? buffer.Length
                    : (int)Math.Min(buffer.Length, stop.Value - total);
                readed = await BaseStream.ReadAsync(buffer, 0, read);
                if (readed <= 0)
                    return total - start;
                var length = ascii.GetBytes(readed.ToString("X"));
                try
                {
                    await stream.WriteAsync(length, 0, length.Length);
                    await stream.WriteAsync(nl, 0, nl.Length);
                    await stream.WriteAsync(buffer, 0, readed);
                    await stream.WriteAsync(nl, 0, nl.Length);
                    total += readed;
                    await stream.FlushAsync();
                }
                catch (IOException)
                {
                    WebServerLog.Add(ServerLogType.Information, GetType(), "write", "connection closed");
                    return total;
                }
            }
            while (readed > 0);
            return total - start;
        }

        protected override async Task<long> ReadStreamInternal(Stream stream, long? length)
        {
            long total = 0;
            int readed, numberLength;
            var ascii = Encoding.ASCII;
            var buffer = new byte[0x10000];
            while (true)
            {
                try { numberLength = await ReadNumber(stream, buffer); }
                catch (IOException)
                {
                    WebServerLog.Add(ServerLogType.Information, GetType(), "read", "connection closed");
                    return total;
                }
                if (numberLength == 0)
                    return total;
                var numberString = ascii.GetString(buffer, 0, numberLength);
                if (!long.TryParse(numberString,
                    NumberStyles.HexNumber,
                    CultureInfo.InvariantCulture.NumberFormat,
                    out long number) || number < 0)
                {
                    WebServerLog.Add(ServerLogType.Information, GetType(), "read", "invalid number of bytes indicator");
                    return total;
                }
                while (number > 0)
                {
                    try { readed = await stream.ReadAsync(buffer, 0, (int)Math.Min(number, buffer.Length)); }
                    catch (IOException)
                    {
                        WebServerLog.Add(ServerLogType.Information, GetType(), "read", "connection closed");
                        return total;
                    }
                    if (readed == 0)
                    {
                        WebServerLog.Add(ServerLogType.Information, GetType(), "read", "could not read the block completly");
                        return total;
                    }
                    await BaseStream.WriteAsync(buffer, 0, readed);
                    total += readed;
                    number -= readed;
                }
                try
                {
                    readed = await stream.ReadAsync(buffer, 0, 1);
                    if (readed > 0 && buffer[0] == '\r')
                        readed = await stream.ReadAsync(buffer, 0, 1);
                    if (readed == 0)
                        return total;
                }
                catch (IOException)
                {
                    WebServerLog.Add(ServerLogType.Information, GetType(), "read", "connection closed");
                    return total;
                }
            }
        }

        private async Task<int> ReadNumber(Stream stream, byte[] buffer)
        {
            int offset = 0;
            var byteBuffer = new byte[1];
            while (true)
            {
                int readed = await stream.ReadAsync(byteBuffer, 0, 1);
                if (readed == 0)
                    return offset;
                if (byteBuffer[0] == '\r' || byteBuffer[0] == '\n')
                {
                    if (byteBuffer[0] == '\r')
                        await stream.ReadAsync(byteBuffer, 0, 1);
                    return offset;
                }
                buffer[offset] = byteBuffer[0];
                offset++;
            }
        }
    }
}
