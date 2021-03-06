﻿using MaxLib.Data;
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

        public override async Task ProgressTask(WebProgressTask task)
        {
            var header = task.Document.ResponseHeader;
            var stream = task.NetworkStream;
            var writer = new StreamWriter(stream);
            await writer.WriteAsync(header.HttpProtocol);
            await writer.WriteAsync(" ");
            await writer.WriteAsync(((int)header.StatusCode).ToString());
            await writer.WriteAsync(" ");
            await writer.WriteLineAsync(StatusCodeText(header.StatusCode));
            for (int i = 0; i < header.HeaderParameter.Count; ++i) //Parameter
            {
                var e = header.HeaderParameter.ElementAt(i);
                await writer.WriteAsync(e.Key);
                await writer.WriteAsync(": ");
                await writer.WriteLineAsync(e.Value);
            }
            foreach (var cookie in task.Document.RequestHeader.Cookie.AddedCookies) //Cookies
            {
                await writer.WriteAsync("Set-Cookie: ");
                await writer.WriteLineAsync(cookie.ToString());
            }
            await writer.WriteLineAsync();
            try { await writer.FlushAsync(); await stream.FlushAsync(); }
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
                        await SendChunk(writer, stream, s);
                    await writer.WriteLineAsync("0");
                    await writer.WriteLineAsync();
                    await writer.FlushAsync();
                    await stream.FlushAsync();
                }
            }
            catch (IOException)
            {
                WebServerLog.Add(ServerLogType.Information, GetType(), "Send", "Connection closed by remote host.");
                return;
            }
        }

        protected virtual async Task SendChunk(StreamWriter writer, Stream stream, HttpDataSource source)
        {
            if (source is LazySource lazySource)
                foreach (var s in lazySource.GetAllSources())
                    await SendChunk(writer, stream, s);
            else if (source is Remote.MarshalSource ms && ms.IsLazy)
                foreach (var s in ms.GetAllSources())
                    await SendChunk(writer, stream, s);
            else if (source is HttpChunkedStream)
            {
                await stream.FlushAsync();
                await writer.FlushAsync();
                await source.WriteStream(stream);
                await stream.FlushAsync();
                await writer.FlushAsync();
            }
            else
            {
                var length = source.Length();
                if (length == null)
                    using (var sink = new BufferedSinkStream())
                    {
                        _ = Task.Run(async () =>
                        {
                            await source.WriteStream(sink);
                            sink.FinishWrite();
                        });
                        await SendChunk(writer, stream, new HttpChunkedStream(sink));
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
                    await writer.WriteLineAsync(length.Value.ToString("X"));
                    await writer.FlushAsync();
                    await source.WriteStream(stream);
                }
                await stream.FlushAsync();
                await writer.WriteLineAsync();
                await writer.FlushAsync();
            }
        }
    }
}
