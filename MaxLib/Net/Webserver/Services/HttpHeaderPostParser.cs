using System;

namespace MaxLib.Net.Webserver.Services
{
    /// <summary>
    /// WebServiceType.PostParseRequest: Ließt alle Informationen aus dem Header aus und analysiert diese. 
    /// Die Headerklasse wird für die weitere Verwendung vorbereitet.
    /// </summary>
    public class HttpHeaderPostParser : WebService
    {
        /// <summary>
        /// WebServiceType.PostParseRequest: Ließt alle Informationen aus dem Header aus und analysiert diese. 
        /// Die Headerklasse wird für die weitere Verwendung vorbereitet.
        /// </summary>
        public HttpHeaderPostParser()
            : base(WebServiceType.PostParseRequest)
        {
            Importance = WebProgressImportance.High;
        }

        public override void ProgressTask(WebProgressTask task)
        {
            _ = task ?? throw new ArgumentNullException(nameof(task));

            var header = task.Document.RequestHeader;
            //Accept
            if (header.HeaderParameter.TryGetValue("Accept", out string value))
            {
                header.FieldAccept.AddRange(value.Split(
                    new[] { ',', ' ', ';' }, StringSplitOptions.RemoveEmptyEntries));
            }
            //Accept-Encoding
            if (header.HeaderParameter.TryGetValue("Accept-Encoding", out value))
            {
                header.FieldAcceptEncoding.AddRange(value.Split(
                    new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries));
            }
            //Connection
            if (header.HeaderParameter.TryGetValue("Connection", out value))
            {
                if (value.ToLower() == "keep-alive") 
                    header.FieldConnection = HttpConnectionType.KeepAlive;
            }
            //Host
            if (header.HeaderParameter.TryGetValue("Host", out value))
            {
                header.Host = value;
            }
            //Cookie
            if (header.HeaderParameter.TryGetValue("Cookie", out value))
            {
                header.Cookie.SetRequestCookieString(value);
            }
            //Session
            Session.SessionManager.Register(task);
        }

        public override bool CanWorkWith(WebProgressTask task)
            => true;
    }
}
