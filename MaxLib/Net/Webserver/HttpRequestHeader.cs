using System;
using System.Collections.Generic;

namespace MaxLib.Net.Webserver
{
    [Serializable]
    public class HttpRequestHeader : HttpHeader
    {
        private string url = "/";
        public string Url
        {
            get => url;
            set
            {
                if (url == null) throw new ArgumentNullException("Url");
                url = value ?? "/";
                Location.SetLocation(url);
            }
        }

        public HttpLocation Location { get; } = new HttpLocation("/");

        private string host = "";
        public string Host
        {
            get => host;
            set => host = value ?? throw new ArgumentNullException("Host");
        }
        public HttpPost Post { get; } = new HttpPost("");
        public List<string> FieldAccept { get; } = new List<string>();
        public List<string> FieldAcceptCharset { get; } = new List<string>();
        public List<string> FieldAcceptEncoding { get; } = new List<string>();
        public HttpConnectionType FieldConnection { get; set; } = HttpConnectionType.Close;
        public HttpCookie Cookie { get; } = new HttpCookie("");

        public string FieldUserAgent
        {
            get => HeaderParameter["User-Agent"];
            set => HeaderParameter["User-Agent"] = value;
        }
    }
}
