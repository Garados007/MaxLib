using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaxLib.Net.Webserver.Lazy
{
    [Serializable]
    public class LazyTask
    {
        [NonSerialized]
        private WebServer server;

        public WebServer Server => server;

        public HttpSession Session { get; private set; }

        public HttpRequestHeader Header { get; private set; }

        public Dictionary<object, object> Information { get; private set; }
        
        public object this[object identifer]
        {
            get { return Information[identifer]; }
            set { Information[identifer] = value; }
        }     

        public LazyTask(WebProgressTask task)
        {
            if (task == null) throw new ArgumentNullException("task");
            server = task.Server;
            Session = task.Session;
            Header = task.Document.RequestHeader;
            Information = task.Document.Information;
        }
    }

    public delegate IEnumerable<HttpDataSource> LazyEventHandler(LazyTask task);

    [Serializable]
    public class LazySource : HttpDataSource
    {
        public LazySource(WebProgressTask task, LazyEventHandler handler)
        {
            this.task = new LazyTask(task ?? throw new ArgumentNullException("task"));
            Handler = handler ?? throw new ArgumentNullException("handler");
        }

        public LazyEventHandler Handler { get; private set; }

        LazyTask task;
        HttpDataSource[] list;

        public IEnumerable<HttpDataSource> GetAllSources()
        {
            return list != null ? list : Handler(task);
        }

        public override long AproximateLength()
        {
            if (list == null) list = GetAllSources().ToArray();
            return list.Sum((s) => s.AproximateLength());
        }

        public override void Dispose()
        {
            if (list != null)
                foreach (var s in list)
                    s.Dispose();
        }

        public override byte[] GetSourcePart(long start, long length)
        {
            if (list == null) list = GetAllSources().ToArray();
            using (var m = new MemoryStream((int)length))
            {
                for (int i = 0; i < list.Length; ++i)
                {
                    var apl = list[i].AproximateLength();
                    if (start < apl)
                    {
                        var rl = Math.Min(length, apl - start);
                        var b = list[i].GetSourcePart(start, rl);
                        length -= rl;
                        m.Write(b, 0, b.Length);
                    }
                    start -= apl;
                }
                return m.ToArray();
            }
        }

        public override long ReadFromStream(Stream networkStream, long readlength)
        {
            return 0;
        }

        public override long ReserveExtraMemory(long bytes)
        {
            return 0;
        }

        public override int WriteSourcePart(byte[] source, long start, long length)
        {
            return 0;
        }

        public override long WriteToStream(Stream networkStream)
        {
            long length = 0;
            foreach (var s in GetAllSources())
                length += s.WriteToStream(networkStream);
            return length;
        }

#pragma warning disable CS0809
        [Obsolete("This is not supported in this class.", true)]
        public override long RangeStart
        {
            get => base.RangeStart;
            set => base.RangeStart = value;
        }

        [Obsolete("This is not supported in this class.", true)]
        public override long RangeEnd
        {
            get => base.RangeEnd;
            set => base.RangeEnd = value;
        }

        [Obsolete("This is not supported in this class.", true)]
        public override bool TransferCompleteData
        {
            get => base.TransferCompleteData;
            set => base.TransferCompleteData = value;
        }
#pragma warning restore CS0809
    }

    public class ChunkedResponseCreateor : WebService
    {
        public bool OnlyWithLazy { get; private set; }

        public ChunkedResponseCreateor(bool onlyWithLazy = false) : base(WebServiceType.PreCreateResponse)
        {
            OnlyWithLazy = onlyWithLazy;
            if (onlyWithLazy) Importance = WebProgressImportance.High;
        }

        public override bool CanWorkWith(WebProgressTask task)
        {
            return !OnlyWithLazy || (task.Document.DataSources.Count > 0 &&
                task.Document.DataSources.Any((s) => s is LazySource ||
                    (s is Remote.MarshalSource ms && ms.IsLazy)
                ));
        }

        public override void ProgressTask(WebProgressTask task)
        {
            var request = task.Document.RequestHeader;
            var response = task.Document.ResponseHeader;
            response.FieldContentType = task.Document.PrimaryMime;
            response.SetActualDate();
            response.HttpProtocol = request.HttpProtocol;
            response.HeaderParameter["Connection"] = "keep-alive";
            response.HeaderParameter["X-UA-Compatible"] = "IE=Edge";
            response.HeaderParameter["Transfer-Encoding"] = "chunked";
            if (task.Document.PrimaryEncoding != null)
                response.HeaderParameter["Content-Type"] += "; charset=" +
                    task.Document.PrimaryEncoding;
            task.Document.Information.Add("block default response creator", true);
        }
    }

    public class ChunkedSender : Services.HttpSender
    {
        public bool OnlyWithLazy { get; private set; }

        public ChunkedSender(bool onlyWithLazy = false) : base()
        {
            OnlyWithLazy = onlyWithLazy;
            if (onlyWithLazy) Importance = WebProgressImportance.High;
        }

        public override bool CanWorkWith(WebProgressTask task)
        {
            return !OnlyWithLazy || (task.Document.DataSources.Count > 0 &&
                task.Document.DataSources.Any((s) => s is LazySource || 
                    (s is Remote.MarshalSource ms && ms.IsLazy)
                ));
        }

        public override void ProgressTask(WebProgressTask task)
        {
            var header = task.Document.ResponseHeader;
            var stream = task.NetworkStream;
            var writer = new StreamWriter(stream);
            writer.Write(header.HttpProtocol);
            writer.Write(" ");
            writer.Write((int)header.StatusCode);
            writer.Write(" ");
            writer.WriteLine(StatusCodeText(header.StatusCode));
            for (int i = 0; i < header.HeaderParameter.Count; ++i) //Parameter
            {
                var e = header.HeaderParameter.ElementAt(i);
                writer.Write(e.Key);
                writer.Write(": ");
                writer.WriteLine(e.Value);
            }
            foreach (var cookie in task.Document.RequestHeader.Cookie.AddedCookies) //Cookies
            {
                writer.Write("Set-Cookie: ");
                writer.WriteLine(cookie.ToString());
            }
            writer.WriteLine();
            try { writer.Flush(); stream.Flush(); }
            catch (ObjectDisposedException)
            {
                WebServerInfo.Add(InfoType.Error, GetType(), "Send", "Connection closed by remote host.");
                return;
            }
            catch (IOException)
            {
                WebServerInfo.Add(InfoType.Error, GetType(), "Send", "Connection closed by remote host.");
                return;
            }
            //Daten senden
            try
            {
                if (!(task.Document.Information.ContainsKey("Only Header") && (bool)task.Document.Information["Only Header"]))
                {
                    foreach (var s in task.Document.DataSources)
                        SendChunk(writer, stream, s);
                    writer.WriteLine("0");
                    writer.WriteLine();
                    writer.Flush();
                    stream.Flush();
                }
            }
            catch (IOException)
            {
                WebServerInfo.Add(InfoType.Error, GetType(), "Send", "Connection closed by remote host.");
                return;
            }
        }

        protected virtual void SendChunk(StreamWriter writer, Stream stream, HttpDataSource source)
        {
            if (source is LazySource)
                foreach (var s in (source as LazySource).GetAllSources())
                    SendChunk(writer, stream, s);
            else if (source is Remote.MarshalSource ms && ms.IsLazy)
                foreach (var s in ms.GetAllSources())
                    SendChunk(writer, stream, s);
            else if (source is HttpChunkedStream)
            {
                stream.Flush();
                writer.Flush();
                source.WriteToStream(stream);
                stream.Flush();
                writer.Flush();
            }
            else
            {
                var length = source.AproximateLength();
                if (length == 0) return;
                writer.WriteLine(length.ToString("X"));
                writer.Flush();
                source.WriteToStream(stream);
                stream.Flush();
                writer.WriteLine();
                writer.Flush();
            }
        }
    }

    [Serializable]
    public class HttpChunkedStream : HttpDataSource
    {
        public HttpChunkedStream(Stream baseStream, int readBufferLength = 0x8000)
        {
            BaseStream = baseStream ?? throw new ArgumentNullException("baseStream");
            ReadBufferLength = readBufferLength;
            if (readBufferLength <= 0) throw new ArgumentOutOfRangeException("readBufferLength");
        }

        public Stream BaseStream { get; private set; }

        public int ReadBufferLength { get; private set; }

        public override long AproximateLength()
        {
            throw new NotSupportedException();
        }

        public override void Dispose()
        {
            BaseStream.Dispose();
        }

        public override byte[] GetSourcePart(long start, long length)
        {
            throw new NotSupportedException();
        }

        public override long ReadFromStream(Stream networkStream, long readlength)
        {
            throw new NotSupportedException();
        }

        public override long ReserveExtraMemory(long bytes)
        {
            throw new NotSupportedException();
        }

        public override int WriteSourcePart(byte[] source, long start, long length)
        {
            throw new NotSupportedException();
        }

        public override long WriteToStream(Stream networkStream)
        {
            long total = 0;
            int readed;
            byte[] buffer = new byte[ReadBufferLength];
            var ascii = Encoding.ASCII;
            var nl = ascii.GetBytes("\r\n");
            while ((readed = BaseStream.Read(buffer, 0, buffer.Length)) > 0)
            {
                var length = ascii.GetBytes(readed.ToString("X"));
                networkStream.Write(length, 0, length.Length);
                networkStream.Write(nl, 0, nl.Length);
                networkStream.Write(buffer, 0, readed);
                networkStream.Write(nl, 0, nl.Length);
                total += readed;
                networkStream.Flush();
            }
            return total;
        }
    }
}
