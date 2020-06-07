using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MaxLib.Net.Webserver.Services
{
    /// <summary>
    /// WebServiceType.PreParseRequest: Liest und parst den Header aus dem Netzwerk-Stream und sammelt alle Informationen
    /// </summary>
    public class HttpHeaderParser : WebService
    {
        static readonly object lockHeaderFile = new object();
        static readonly object lockRequestFile = new object();

        /// <summary>
        /// WebServiceType.PreParseRequest: Liest und parst den Header aus dem Netzwerk-Stream und sammelt alle Informationen
        /// </summary>
        public HttpHeaderParser() : base(WebServiceType.PreParseRequest) { }

        public override async Task ProgressTask(WebProgressTask task)
        {
            _ = task ?? throw new ArgumentNullException(nameof(task));

            var header = task.Document.RequestHeader;
            var sb = new StringBuilder();
            if (task.NetworkStream == null)
                task.NetworkStream = new HttpStream(task.Session.NetworkClient.GetStream());

            if (task.Server.Settings.Debug_WriteRequests)
            {
                sb.AppendLine(new string('=', 100));
                var date = WebServerUtils.GetDateString(DateTime.Now);
                sb.AppendLine("=   " + date + new string(' ', 95 - date.Length) + "=");
                sb.AppendLine(new string('=', 100));
                sb.AppendLine();
            }

            var cancel = new CancellationTokenSource(5000);
            string line;
            try { line = await task.NetworkStream.ReadLineAsync(cancel.Token); }
            catch
            {
                WebServerLog.Add(ServerLogType.Error, GetType(), "Header", "Connection closed by remote host");
                task.Document.ResponseHeader.StatusCode = HttpStateCode.RequestTimeOut;
                task.NextTask = task.CurrentTask = WebServiceType.SendResponse;
                return;
            }
            finally { cancel.Dispose(); }

            if (line == null)
            {
                WebServerLog.Add(ServerLogType.Error, GetType(), "Header", "Can't read Header line");
                task.Document.ResponseHeader.StatusCode = HttpStateCode.BadRequest;
                task.NextTask = WebServiceType.PreCreateResponse;
                return;
            }
            try
            {
                if (task.Server.Settings.Debug_WriteRequests) 
                    sb.AppendLine(line);
                var parts = line.Split(' ');
                WebServerLog.Add(ServerLogType.Debug, GetType(), "Header", line);
                header.ProtocolMethod = parts[0];
                header.Url = parts[1];
                header.HttpProtocol = parts[2];
                while (!string.IsNullOrWhiteSpace(line = await task.NetworkStream.ReadLineAsync()))
                {
                    if (task.Server.Settings.Debug_WriteRequests) 
                        sb.AppendLine(line);
                    var ind = line.IndexOf(':');
                    var key = line.Remove(ind);
                    var value = line.Substring(ind + 1).Trim();
                    header.HeaderParameter.Add(key, value);
                }
                if (task.Server.Settings.Debug_WriteRequests) sb.AppendLine();
            }
            catch
            {
                WebServerLog.Add(ServerLogType.Error, GetType(), "Header", "Bad Request");
                task.Document.ResponseHeader.StatusCode = HttpStateCode.BadRequest;
                task.NextTask = WebServiceType.PreCreateResponse;
                return;
            }
            if (header.HeaderParameter.ContainsKey("Content-Length"))
            {
                var buffer = new byte[int.Parse(header.HeaderParameter["Content-Length"])];
                _ = await task.NetworkStream.ReadAsync(buffer, 0, buffer.Length);
                header.Post.SetPost(Encoding.UTF8.GetString(buffer), 
                    header.HeaderParameter.TryGetValue("Content-Type", out string mime) ? mime : null);
                if (task.Server.Settings.Debug_WriteRequests) 
                    sb.AppendLine(Encoding.UTF8.GetString(buffer));
            }
            if (task.Server.Settings.Debug_WriteRequests)
            {
                sb.AppendLine(); sb.AppendLine();
                lock (lockHeaderFile) File.AppendAllText("headers.txt", sb.ToString());
            }
            if (task.Server.Settings.Debug_LogConnections)
            {
                sb = new StringBuilder();
                sb.AppendLine(WebServerUtils.GetDateString(DateTime.Now) + "  " +
                    task.Session.NetworkClient.Client.RemoteEndPoint.ToString());
                var host = header.HeaderParameter.ContainsKey("Host") ? header.HeaderParameter["Host"] : "";
                sb.AppendLine("    " + host + task.Document.RequestHeader.Location.DocumentPath);
                sb.AppendLine();
                lock (lockRequestFile) File.AppendAllText("requests.txt", sb.ToString());
            }
        }

        public override bool CanWorkWith(WebProgressTask task)
            => true;
    }
}
