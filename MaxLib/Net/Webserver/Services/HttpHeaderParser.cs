using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;

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

        public override void ProgressTask(WebProgressTask task)
        {
            var header = task.Document.RequestHeader;
            var stream = task.NetworkStream;
            var reader = new StreamReader(stream);
            var mwt = 50;
            var sb = new StringBuilder();
            if (!(stream is NetworkStream))
                stream = task.Session.NetworkClient.GetStream();
            if (task.Server.Settings.Debug_WriteRequests)
            {
                sb.AppendLine(new string('=', 100));
                var date = WebServerHelper.GetDateString(DateTime.Now);
                sb.AppendLine("=   " + date + new string(' ', 95 - date.Length) + "=");
                sb.AppendLine(new string('=', 100));
                sb.AppendLine();
            }
            while (!((NetworkStream)stream).DataAvailable && mwt > 0)
            {
                Thread.Sleep(100);
                mwt--;
                if (!task.Session.NetworkClient.Connected) return;
            }
            try
            {
                if (!((NetworkStream)stream).DataAvailable)
                {
                    task.Document.RequestHeader.FieldConnection = HttpConnectionType.KeepAlive;
                    WebServerInfo.Add(InfoType.Error, GetType(), "Header", "Request Time out");
                    task.Document.ResponseHeader.StatusCode = HttpStateCode.RequestTimeOut;
                    task.NextTask = WebServiceType.PreCreateResponse;
                    return;
                }
            }
            catch (ObjectDisposedException)
            {
                WebServerInfo.Add(InfoType.Error, GetType(), "Header", "Connection closed by remote host");
                task.Document.ResponseHeader.StatusCode = HttpStateCode.RequestTimeOut;
                task.NextTask = task.CurrentTask = WebServiceType.SendResponse;
                return;
            }
            string line;
            try { line = reader.ReadLine(); }
            catch
            {
                WebServerInfo.Add(InfoType.Error, GetType(), "Header", "Connection closed by remote host");
                task.Document.ResponseHeader.StatusCode = HttpStateCode.RequestTimeOut;
                task.NextTask = task.CurrentTask = WebServiceType.SendResponse;
                return;
            }
            if (line == null)
            {
                WebServerInfo.Add(InfoType.Error, GetType(), "Header", "Can't read Header line");
                task.Document.ResponseHeader.StatusCode = HttpStateCode.BadRequest;
                task.NextTask = WebServiceType.PreCreateResponse;
                return;
            }
            try
            {
                if (task.Server.Settings.Debug_WriteRequests) sb.AppendLine(line);
                var parts = line.Split(' ');
                WebServerInfo.Add(InfoType.Debug, GetType(), "Header", line);
                header.ProtocolMethod = parts[0];
                header.Url = parts[1];
                header.HttpProtocol = parts[2];
                while (!string.IsNullOrWhiteSpace(line = reader.ReadLine()))
                {
                    if (task.Server.Settings.Debug_WriteRequests) sb.AppendLine(line);
                    var ind = line.IndexOf(':');
                    var key = line.Remove(ind);
                    var value = line.Substring(ind + 1).Trim();
                    header.HeaderParameter.Add(key, value);
                }
                if (task.Server.Settings.Debug_WriteRequests) sb.AppendLine();
            }
            catch
            {
                WebServerInfo.Add(InfoType.Error, GetType(), "Header", "Bad Request");
                task.Document.ResponseHeader.StatusCode = HttpStateCode.BadRequest;
                task.NextTask = WebServiceType.PreCreateResponse;
                return;
            }
            if (header.ProtocolMethod == HttpProtocollMethod.Post)
            {
                var buffer = new char[int.Parse(header.HeaderParameter["Content-Length"])];
                _ = reader.ReadBlock(buffer, 0, buffer.Length);
                header.Post.SetPost(new string(buffer));
                if (task.Server.Settings.Debug_WriteRequests) sb.AppendLine(new string(buffer));
            }
            if (task.Server.Settings.Debug_WriteRequests)
            {
                sb.AppendLine(); sb.AppendLine();
                lock (lockHeaderFile) File.AppendAllText("headers.txt", sb.ToString());
            }
            if (task.Server.Settings.Debug_LogConnections)
            {
                sb = new StringBuilder();
                sb.AppendLine(WebServerHelper.GetDateString(DateTime.Now) + "  " +
                    task.Session.NetworkClient.Client.RemoteEndPoint.ToString());
                var host = header.HeaderParameter.ContainsKey("Host") ? header.HeaderParameter["Host"] : "";
                sb.AppendLine("    " + host + task.Document.RequestHeader.Location.DocumentPath);
                sb.AppendLine();
                lock (lockRequestFile) File.AppendAllText("requests.txt", sb.ToString());
            }
        }

        public override bool CanWorkWith(WebProgressTask task)
        {
            return true;
        }
    }
}
