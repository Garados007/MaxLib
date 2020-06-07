using System;
using System.Collections.Generic;

namespace MaxLib.Net.Webserver
{
    [Serializable]
    public class HttpRequestHeader : HttpHeader
    {
        public string Url
        {
            get => Location.Url;
            set => Location.SetLocation(value ?? "/");
        }

        public HttpLocation Location { get; } = new HttpLocation("/");

        private string host = "";
        public string Host
        {
            get => host;
            set => host = value ?? throw new ArgumentNullException(nameof(Host));
        }
        public HttpPost Post { get; } = new HttpPost("", null);
        public List<string> FieldAccept { get; } = new List<string>();
        public List<string> FieldAcceptCharset { get; } = new List<string>();
        public List<string> FieldAcceptEncoding { get; } = new List<string>();
        public HttpConnectionType FieldConnection { get; set; } = HttpConnectionType.Close;
        public HttpCookie Cookie { get; } = new HttpCookie("");

        public string FieldUserAgent
        {
            get => HeaderParameter.TryGetValue("User-Agent", out string value) ? value : null;
            set => HeaderParameter["User-Agent"] = value;
        }
    }
}
