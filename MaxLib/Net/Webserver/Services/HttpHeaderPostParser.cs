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
            var header = task.Document.RequestHeader;
            //Accept
            if (header.HeaderParameter.ContainsKey("Accept"))
            {
                var tiles = header.HeaderParameter["Accept"].Split(
                    new[] { ',', ' ', ';' }, StringSplitOptions.RemoveEmptyEntries);
                header.FieldAccept.AddRange(tiles);
            }
            //Accept-Encoding
            if (header.HeaderParameter.ContainsKey("Accept-Encoding"))
            {
                var tiles = header.HeaderParameter["Accept-Encoding"].Split(
                    new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                header.FieldAcceptEncoding.AddRange(tiles);
            }
            //Connection
            if (header.HeaderParameter.ContainsKey("Connection"))
            {
                var text = header.HeaderParameter["Connection"].ToLower();
                if (text == "keep-alive") header.FieldConnection =
                    HttpConnectionType.KeepAlive;
            }
            //Host
            if (header.HeaderParameter.ContainsKey("Host"))
            {
                header.Host = header.HeaderParameter["Host"];
            }
            //Cookie
            if (header.HeaderParameter.ContainsKey("Cookie"))
            {
                header.Cookie.SetRequestCookieString(header.HeaderParameter["Cookie"]);
            }
            //Session
            Session.SessionManager.Register(task);
        }

        public override bool CanWorkWith(WebProgressTask task)
        {
            return true;
        }
    }
}
