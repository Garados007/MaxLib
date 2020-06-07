using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MaxLib.Net.Webserver.Services
{
    /// <summary>
    /// WebServiceType.SendResponse: Sendet Response und Dokument, wenn vorhanden, an den Clienten.
    /// </summary>
    public class HttpSender : WebService
    {
        /// <summary>
        /// WebServiceType.SendResponse: Sendet Response und Dokument, wenn vorhanden, an den Clienten.
        /// </summary>
        public HttpSender() : base(WebServiceType.SendResponse) { }

        public virtual string StatusCodeText(HttpStateCode code)
        {
            switch ((int)code)
            {
                case 100: return "Continue";
                case 101: return "Switching Protcols";
                case 102: return "Processing";
                case 200: return "OK";
                case 201: return "Created";
                case 202: return "Accepted";
                case 203: return "Non-Authoritative Information";
                case 204: return "No Content";
                case 205: return "Reset Content";
                case 206: return "Partial Content";
                case 207: return "Multi-Status";
                case 208: return "IM Used";
                case 300: return "Multiple Choises";
                case 301: return "Moved Permanently";
                case 302: return "Found";
                case 303: return "See Other";
                case 304: return "Not Modified";
                case 305: return "Use Proxy";
                case 307: return "Temporary Redirect";
                case 308: return "Permanent Redirect";
                case 400: return "Bad Request";
                case 401: return "Unathorized";
                case 402: return "Payment Required";
                case 403: return "Forbidden";
                case 404: return "Not Found";
                case 405: return "Method Not Allowed";
                case 406: return "Not Acceptable";
                case 407: return "Proxy Authendtication Required";
                case 408: return "Request Time-out";
                case 409: return "Conflict";
                case 410: return "Gone";
                case 411: return "Length Required";
                case 412: return "Precondition Failed";
                case 413: return "Request Entity Too Large";
                case 414: return "Request-URL Too Long";
                case 415: return "Unsupported Media Type";
                case 416: return "Requested range not satisfiable";
                case 417: return "Expectation Failed";
                case 418: return "I'm a teapot";
                case 420: return "Policy Not Fulfilled";
                case 421: return "There are too many connections from your internet address";
                case 422: return "Unprocessable Entity";
                case 423: return "Locked";
                case 424: return "Failed Dependency";
                case 425: return "Unordered Collection";
                case 426: return "Upgrade Required";
                case 428: return "Precondition Required";
                case 429: return "Too Many Requests";
                case 431: return "Request Header Fields Too Large";
                case 500: return "Internal Server Error";
                case 501: return "Not Implemented";
                case 502: return "Bad Gateway";
                case 503: return "Service Unavailable";
                case 504: return "Gateway Time-out";
                case 505: return "HTTP Version not supported";
                case 506: return "Variant Also Negotiates";
                case 507: return "Insufficient Storage";
                case 508: return "Loop Detected";
                case 509: return "Bandwidth Limit Exceeded";
                case 510: return "Not Extended";
                default:
                    WebServerLog.Add(ServerLogType.Information, GetType(), "StatusCode",
                        "Cant get status string from {0} ({1}).", code, (int)code);
                    return "";
            }
        }

        public override async Task ProgressTask(WebProgressTask task)
        {
            _ = task ?? throw new ArgumentNullException(nameof(task));

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
            try { await writer.FlushAsync(); }
            catch (ObjectDisposedException)
            {
                WebServerLog.Add(ServerLogType.Error, GetType(), "Send", "Connection closed by remote host.");
                return;
            }
            catch (IOException)
            {
                WebServerLog.Add(ServerLogType.Error, GetType(), "Send", "Connection closed by remote host.");
                return;
            }
            //Daten senden
            if (!(task.Document.Information.ContainsKey("Only Header") && (bool)task.Document.Information["Only Header"]))
                for (int i = 0; i < task.Document.DataSources.Count; ++i)
                {
                    await task.Document.DataSources[i].WriteStream(stream);
                }
            try { await stream.FlushAsync(); }
            catch (IOException)
            {
                WebServerLog.Add(ServerLogType.Error, GetType(), "Send", "Connection closed by remote host.");
                return;
            }
        }

        public override bool CanWorkWith(WebProgressTask task)
            => true;
    }
}
