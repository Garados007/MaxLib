using MaxLib.Data;
using MaxLib.Net.Webserver.Lazy;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MaxLib.Net.Webserver.Chunked
{
    public class ChunkedSender : Services.HttpSender
    {
        public bool OnlyWithLazy { get; private set; }

        public ChunkedSender(bool onlyWithLazy = false) : base()
        {
            OnlyWithLazy = onlyWithLazy;
            if (onlyWithLazy) 
                Importance = WebProgressImportance.High;
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
                WebServerLog.Add(ServerLogType.Information, GetType(), "Send", "Connection closed by remote host.");
                return;
            }
            catch (IOException)
            {
                WebServerLog.Add(ServerLogType.Information, GetType(), "Send", "Connection closed by remote host.");
                return;
            }
            //send data
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
                WebServerLog.Add(ServerLogType.Information, GetType(), "Send", "Connection closed by remote host.");
                return;
            }
        }

        protected virtual void SendChunk(StreamWriter writer, Stream stream, HttpDataSource source)
        {
            if (source is LazySource lazySource)
                foreach (var s in lazySource.GetAllSources())
                    SendChunk(writer, stream, s);
            else if (source is Remote.MarshalSource ms && ms.IsLazy)
                foreach (var s in ms.GetAllSources())
                    SendChunk(writer, stream, s);
            else if (source is HttpChunkedStream)
            {
                stream.Flush();
                writer.Flush();
                source.WriteStream(stream);
                stream.Flush();
                writer.Flush();
            }
            else
            {
                var length = source.Length();
                if (length == null)
                    using (var sink = new BufferedSinkStream())
                    {
                        _ = Task.Run(() =>
                        {
                            source.WriteStream(sink);
                            sink.FinishWrite();
                        });
                        SendChunk(writer, stream, new HttpChunkedStream(sink));
                    }
                //using (var m = new MemoryStream())
                //{
                //    source.WriteStream(m);
                //    if (m.Length == 0)
                //        return;
                //    writer.WriteLine(m.Length.ToString("X"));
                //    writer.Flush();
                //    m.Position = 0;
                //    m.WriteTo(stream);
                //}
                else
                {
                    if (length.Value == 0) return;
                    writer.WriteLine(length.Value.ToString("X"));
                    writer.Flush();
                    source.WriteStream(stream);
                }
                stream.Flush();
                writer.WriteLine();
                writer.Flush();
            }
        }
    }
}
